// -----------------------------------------------------------------------
// Artigo: .NET Native AOT: JIT, R2R e AOT em Benchmarks
// URL: https://zocate.li/posts/2026/dotnet-native-aot-jit-r2r-benchmarks/
// Coleta publish, tamanho e startup com processos novos e ordem randomizada.
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlogSamples.NativeAot.DecisionLab.Harness;

const string runtimeIdentifier = "linux-x64";
string[] profiles = ["jit-fdd", "jit-scd", "r2r-scd", "native-aot"];
HarnessOptions options = ParseOptions(args);

if (!OperatingSystem.IsLinux())
{
    throw new PlatformNotSupportedException("O baseline linux-x64 deve ser coletado no container documentado no README.");
}

string apiProject = Path.Combine(
    options.RepositoryRoot,
    "src",
    "BlogSamples",
    "NativeAot",
    "DecisionLab",
    "DecisionLab.Api",
    "DecisionLab.Api.csproj");
Directory.CreateDirectory(options.OutputDirectory);

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

var publishRows = new List<PublishMeasurement>();
if (!options.SkipPublish)
{
    Console.WriteLine("Preparando assets do container fora da medição...");
    await ProcessRunner.RunAsync(
        "dotnet",
        ["restore", apiProject, "--runtime", runtimeIdentifier],
        options.RepositoryRoot,
        cancellationSource.Token);

    foreach (string profile in profiles)
    {
        for (int repetition = 1; repetition <= options.PublishRepetitions; repetition++)
        {
            string publishDirectory = GetPublishDirectory(options.RepositoryRoot, profile);
            await ProcessRunner.RunAsync(
                "dotnet",
                ["clean", apiProject, "--configuration", "Release", "--runtime", runtimeIdentifier, "--verbosity", "quiet"],
                options.RepositoryRoot,
                cancellationSource.Token);
            RecreateDirectory(publishDirectory);
            Console.WriteLine($"Restaurando dependências do perfil {profile} fora da medição...");
            await ProcessRunner.RunAsync(
                "dotnet",
                ["restore", apiProject, "--runtime", runtimeIdentifier, $"/p:PublishProfile={profile}"],
                options.RepositoryRoot,
                cancellationSource.Token);

            publishRows.Add(await MeasurePublishAsync(profile, "clean", repetition, apiProject, publishDirectory));
            publishRows.Add(await MeasurePublishAsync(profile, "incremental", repetition, apiProject, publishDirectory));
        }
    }

    await CsvWriter.WritePublishAsync(Path.Combine(options.OutputDirectory, "publish-raw.csv"), publishRows);
    await File.WriteAllTextAsync(
        Path.Combine(options.OutputDirectory, "publish-raw.json"),
        JsonSerializer.Serialize(publishRows, JsonOptions()),
        cancellationSource.Token);

    PublishMeasurement[] failedPublishes = publishRows.Where(row => !row.Succeeded).ToArray();
    if (failedPublishes.Length > 0)
    {
        throw new InvalidOperationException(
            $"{failedPublishes.Length} publish(es) falharam. Consulte publish-raw.csv antes de medir startup.");
    }
}

    string startupArtifactsRoot = Path.Combine(Path.GetTempPath(), $"decision-lab-startup-{Guid.NewGuid():N}");
    foreach (string profile in profiles)
    {
        CopyDirectory(
        GetPublishDirectory(options.RepositoryRoot, profile),
        Path.Combine(startupArtifactsRoot, profile));
    }

var startupPlan = profiles
    .SelectMany(profile => Enumerable.Range(1, options.StartupRepetitions).Select(repetition => (profile, repetition)))
    .ToList();
Shuffle(startupPlan, new Random(options.RandomSeed));

var startupRows = new List<StartupMeasurement>();
for (int index = 0; index < startupPlan.Count; index++)
{
    (string profile, int repetition) = startupPlan[index];
    StartupMeasurement measurement = await MeasureStartupAsync(
        profile,
        repetition,
        index + 1,
        startupArtifactsRoot,
        cancellationSource.Token);
    startupRows.Add(measurement);
    Console.WriteLine($"startup {index + 1}/{startupPlan.Count}: {profile} = {measurement.DurationMilliseconds:F3} ms");
}

await CsvWriter.WriteStartupAsync(Path.Combine(options.OutputDirectory, "startup-raw.csv"), startupRows);
await File.WriteAllTextAsync(
    Path.Combine(options.OutputDirectory, "startup-raw.json"),
    JsonSerializer.Serialize(startupRows, JsonOptions()),
    cancellationSource.Token);

ExperimentManifest manifest = await CreateManifestAsync(options, profiles, cancellationSource.Token);
await File.WriteAllTextAsync(
    Path.Combine(options.OutputDirectory, "manifest.json"),
    JsonSerializer.Serialize(manifest, JsonOptions()),
    cancellationSource.Token);
Directory.Delete(startupArtifactsRoot, recursive: true);

Console.WriteLine($"Resultados brutos gravados em {options.OutputDirectory}");

async Task<PublishMeasurement> MeasurePublishAsync(
    string profile,
    string category,
    int repetition,
    string project,
    string publishDirectory)
{
    try
    {
        (TimeSpan duration, _) = await ProcessRunner.RunAsync(
            "dotnet",
            ["publish", project, "--configuration", "Release", "--runtime", runtimeIdentifier,
                "--no-restore", $"/p:PublishProfile={profile}", "--verbosity", "quiet"],
            options.RepositoryRoot,
            cancellationSource.Token);
        long uncompressedBytes = Directory.EnumerateFiles(publishDirectory, "*", SearchOption.AllDirectories)
            .Sum(path => new FileInfo(path).Length);
        long compressedBytes = MeasureCompressedSize(publishDirectory);
        Console.WriteLine($"publish {profile}/{category}/{repetition}: {duration.TotalMilliseconds:F3} ms");
        return new(profile, category, repetition, duration.TotalMilliseconds, uncompressedBytes, compressedBytes, true, null);
    }
    catch (Exception exception)
    {
        return new(profile, category, repetition, 0, 0, 0, false, exception.Message);
    }
}

static async Task<StartupMeasurement> MeasureStartupAsync(
    string profile,
    int repetition,
    int executionOrder,
    string startupArtifactsRoot,
    CancellationToken cancellationToken)
{
    int port = GetAvailablePort();
    string publishDirectory = Path.Combine(startupArtifactsRoot, profile);
    bool frameworkDependent = profile == "jit-fdd";
    string executable = frameworkDependent ? "dotnet" : Path.Combine(publishDirectory, "DecisionLab.Api");

    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = publishDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        },
    };

    if (frameworkDependent)
    {
        process.StartInfo.ArgumentList.Add(Path.Combine(publishDirectory, "DecisionLab.Api.dll"));
    }

    process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
    process.StartInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
    process.StartInfo.Environment["DOTNET_NOLOGO"] = "1";

    var stopwatch = Stopwatch.StartNew();
    bool started = false;
    try
    {
        started = process.Start();
        if (!started)
        {
            throw new InvalidOperationException("Process.Start retornou false.");
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(250) };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (true)
        {
            timeout.Token.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                string error = await process.StandardError.ReadToEndAsync(timeout.Token);
                throw new InvalidOperationException($"Processo encerrou antes de /ready: {error}");
            }

            try
            {
                using HttpResponseMessage response = await client.GetAsync($"http://127.0.0.1:{port}/ready", timeout.Token);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    stopwatch.Stop();
                    return new(profile, repetition, executionOrder, stopwatch.Elapsed.TotalMilliseconds, true, null);
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
            }
        }
    }
    catch (Exception exception)
    {
        stopwatch.Stop();
        return new(profile, repetition, executionOrder, stopwatch.Elapsed.TotalMilliseconds, false, exception.Message);
    }
    finally
    {
        if (started && !process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
    }
}

static HarnessOptions ParseOptions(string[] commandLineArguments)
{
    string repositoryRoot = Directory.GetCurrentDirectory();
    int publishRepetitions = 3;
    int startupRepetitions = 30;
    int randomSeed = 20260901;
    bool skipPublish = false;
    string? output = null;

    for (int index = 0; index < commandLineArguments.Length; index++)
    {
        string option = commandLineArguments[index];
        if (option == "--skip-publish")
        {
            skipPublish = true;
            continue;
        }

        if (++index >= commandLineArguments.Length)
        {
            throw new ArgumentException($"Valor ausente para {option}.");
        }

        string value = commandLineArguments[index];
        switch (option)
        {
            case "--repository-root": repositoryRoot = Path.GetFullPath(value); break;
            case "--output": output = Path.GetFullPath(value); break;
            case "--publish-repetitions": publishRepetitions = int.Parse(value); break;
            case "--startup-repetitions": startupRepetitions = int.Parse(value); break;
            case "--seed": randomSeed = int.Parse(value); break;
            default: throw new ArgumentException($"Opção desconhecida: {option}.");
        }
    }

    output ??= Path.Combine(
        repositoryRoot,
        "src",
        "BlogSamples",
        "NativeAot",
        "DecisionLab",
        "results",
        Environment.MachineName.ToLowerInvariant(),
        DateTime.UtcNow.ToString("yyyy-MM-dd"));

    return new(repositoryRoot, output, publishRepetitions, startupRepetitions, randomSeed, skipPublish);
}

static async Task<ExperimentManifest> CreateManifestAsync(
    HarnessOptions harnessOptions,
    string[] manifestProfiles,
    CancellationToken cancellationToken)
{
    (_, string dotnetInfo) = await ProcessRunner.RunAsync(
        "dotnet", ["--info"], harnessOptions.RepositoryRoot, cancellationToken);
    (_, string commit) = await ProcessRunner.RunAsync(
        "git", ["rev-parse", "HEAD"], harnessOptions.RepositoryRoot, cancellationToken);
    (_, string status) = await ProcessRunner.RunAsync(
        "git", ["status", "--short"], harnessOptions.RepositoryRoot, cancellationToken);

    string processor = File.Exists("/proc/cpuinfo")
        ? File.ReadLines("/proc/cpuinfo")
            .FirstOrDefault(line => line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))?
            .Split(':', 2)[1].Trim() ?? "unknown"
        : "unknown";

    return new(
        DateTimeOffset.UtcNow,
        commit.Trim(),
        !string.IsNullOrWhiteSpace(status),
        CalculateSourceFingerprint(harnessOptions.RepositoryRoot),
        RuntimeInformation.OSDescription,
        RuntimeInformation.ProcessArchitecture.ToString(),
        processor,
        GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
        dotnetInfo,
        runtimeIdentifier,
        "Release/Production",
        GCSettings.IsServerGC ? "server" : "workstation",
        harnessOptions.PublishRepetitions,
        harnessOptions.StartupRepetitions,
        harnessOptions.RandomSeed,
        manifestProfiles,
        ["mesmo código", "mesmo RID", "mesmo logging", "mesmo GC", "mesmo payload"],
        ["CPU e alocação determinísticas", "System.Text.Json source generation", "sem I/O externo", "sem código dinâmico deliberado"],
        Environment.CommandLine);
}

static string CalculateSourceFingerprint(string repositoryRoot)
{
    string labRoot = Path.Combine(repositoryRoot, "src", "BlogSamples", "NativeAot", "DecisionLab");
    string[] extensions = [".cs", ".csproj", ".pubxml", ".py", ".yml", ".md"];
    string[] files = Directory.EnumerateFiles(labRoot, "*", SearchOption.AllDirectories)
        .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
            || Path.GetFileName(path).Equals("Dockerfile", StringComparison.Ordinal))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !path.Contains($"{Path.DirectorySeparatorChar}results{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .ToArray();

    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach (string file in files)
    {
        string relativePath = Path.GetRelativePath(labRoot, file).Replace('\\', '/');
        hash.AppendData(Encoding.UTF8.GetBytes(relativePath + "\n"));
        hash.AppendData(File.ReadAllBytes(file));
    }

    return Convert.ToHexStringLower(hash.GetHashAndReset());
}

static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true };

static string GetPublishDirectory(string repositoryRoot, string profile) => Path.Combine(
    repositoryRoot,
    "src",
    "BlogSamples",
    "NativeAot",
    "DecisionLab",
    "artifacts",
    "publish",
    profile);

static void RecreateDirectory(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }

    Directory.CreateDirectory(path);
}

static void CopyDirectory(string source, string destination)
{
    Directory.CreateDirectory(destination);

    foreach (string sourceFile in Directory.EnumerateFiles(source))
    {
        File.Copy(sourceFile, Path.Combine(destination, Path.GetFileName(sourceFile)), overwrite: true);
    }

    foreach (string sourceDirectory in Directory.EnumerateDirectories(source))
    {
        CopyDirectory(sourceDirectory, Path.Combine(destination, Path.GetFileName(sourceDirectory)));
    }
}

static long MeasureCompressedSize(string directory)
{
    string archive = Path.Combine(Path.GetTempPath(), $"decision-lab-{Guid.NewGuid():N}.zip");
    try
    {
        ZipFile.CreateFromDirectory(directory, archive, CompressionLevel.SmallestSize, includeBaseDirectory: false);
        return new FileInfo(archive).Length;
    }
    finally
    {
        File.Delete(archive);
    }
}

static int GetAvailablePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static void Shuffle<T>(IList<T> values, Random random)
{
    for (int index = values.Count - 1; index > 0; index--)
    {
        int swapIndex = random.Next(index + 1);
        (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
    }
}