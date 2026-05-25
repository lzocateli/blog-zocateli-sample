// -----------------------------------------------------------------------
// Artigo: C# como Script no .NET: Como Usar e Quando Vale a Pena
// URL: https://zocate.li/posts/2026/executar-csharp-como-script-dotnet/
// Demonstra dotnet run arquivo.cs — novidade do .NET 10.
// Executa sem .csproj, sem solução, sem configuração adicional.
//
// Execução (requer .NET 10 SDK):
//   dotnet run verificar-versao.cs
//   dotnet run verificar-versao.cs -- dev staging prod
//
// NOTA: Este arquivo .cs está EXCLUÍDO da compilação do BlogSamples.csproj
// via <Compile Remove="Scripting/DotnetRunSingleFile/**" />.
// Deve ser executado diretamente com "dotnet run verificar-versao.cs".
// -----------------------------------------------------------------------

// Argumentos de linha de comando — disponíveis via args implicitamente
var ambientes = args.Length > 0
    ? args
    : new[] { "dev", "staging" };

Console.WriteLine("=== Verificador de Versão por Ambiente ===");
Console.WriteLine($"Ambientes: {string.Join(", ", ambientes)}");
Console.WriteLine();

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

foreach (var env in ambientes)
{
    var url = $"https://api.{env}.empresa.com/health";
    try
    {
        var resposta = await http.GetStringAsync(url);
        Console.WriteLine($"  {env,-10} → ✓ {resposta}");
    }
    catch (HttpRequestException ex)
    {
        // Em desenvolvimento, a URL é fictícia — exibe o erro como esperado
        Console.WriteLine($"  {env,-10} → ✗ {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Concluído em {DateTime.Now:HH:mm:ss}");
