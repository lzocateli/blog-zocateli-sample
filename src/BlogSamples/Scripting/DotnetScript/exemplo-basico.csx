#!/usr/bin/env dotnet-script
// -----------------------------------------------------------------------
// Artigo: C# como Script no .NET: Como Usar e Quando Vale a Pena
// URL: https://zocate.li/posts/2026/executar-csharp-como-script-dotnet/
// Script .csx básico — execução sem projeto, sem csproj.
//
// Execução:
//   dotnet script exemplo-basico.csx
//   dotnet script exemplo-basico.csx -- producao    (passando argumento)
//
// Linux/macOS (com shebang):
//   chmod +x exemplo-basico.csx
//   ./exemplo-basico.csx
// -----------------------------------------------------------------------

// Args estão disponíveis via Args (IReadOnlyList<string> no dotnet-script)
var ambiente = Args.Count > 0 ? Args[0] : "desenvolvimento";

Console.WriteLine("=== Script C# com dotnet-script ===");
Console.WriteLine($"Ambiente    : {ambiente}");
Console.WriteLine($"Data/hora   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Usuário     : {Environment.UserName}");
Console.WriteLine($"Máquina     : {Environment.MachineName}");
Console.WriteLine($".NET version: {Environment.Version}");
Console.WriteLine();

// Demonstração de async/await — suportado nativamente em .csx
async Task<string> ObterStatusAsync(string url)
{
    using var http = new System.Net.Http.HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    try
    {
        var resposta = await http.GetAsync(url);
        return resposta.IsSuccessStatusCode ? "✓ OK" : $"✗ {(int)resposta.StatusCode}";
    }
    catch (Exception ex)
    {
        return $"✗ Erro: {ex.Message}";
    }
}

// Verificar conectividade com a internet
var status = await ObterStatusAsync("https://httpbin.org/status/200");
Console.WriteLine($"Conectividade: {status}");
