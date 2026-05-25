// -----------------------------------------------------------------------
// Artigo: C# como Script no .NET: Como Usar e Quando Vale a Pena
// URL: https://zocate.li/posts/2026/executar-csharp-como-script-dotnet/
// Demonstra top-level statements: eliminação do boilerplate de
// class Program / static void Main() — requer .csproj (mínimo).
// -----------------------------------------------------------------------
//
// Para usar top-level statements, crie um projeto console mínimo:
//
//   dotnet new console -n MeuScript --use-program-main false
//   cd MeuScript
//
// O arquivo Program.cs gerado já usa top-level statements.
// Todo o código abaixo estaria diretamente no Program.cs, sem
// namespace, sem class, sem Main.
//
// -----------------------------------------------------------------------
// EXEMPLO 1 — "Olá mundo" clássico vs top-level
// -----------------------------------------------------------------------
//
// Antes (C# 8 e anteriores):
//
//   using System;
//   namespace MeuApp
//   {
//       class Program
//       {
//           static void Main(string[] args)
//           {
//               Console.WriteLine("Olá, mundo!");
//           }
//       }
//   }
//
// Depois (C# 9+ com top-level statements) — o compilador gera o resto:
//
//   Console.WriteLine("Olá, mundo!");
//
// -----------------------------------------------------------------------
// EXEMPLO 2 — Argumentos de linha de comando
// -----------------------------------------------------------------------
//
// O array "args" está disponível implicitamente sem declaração:
//
//   var ambiente = args.Length > 0 ? args[0] : "desenvolvimento";
//   Console.WriteLine($"Executando em: {ambiente}");
//
//   // Execução:  dotnet run -- staging
//
// -----------------------------------------------------------------------
// EXEMPLO 3 — Script completo com async/await e HttpClient
// -----------------------------------------------------------------------
//
// Top-level statements suportam await nativamente — o compilador
// gera um método Main assíncrono automaticamente:
//
//   using System.Net.Http;
//
//   var ambiente = args.Length > 0 ? args[0] : "dev";
//   var url = $"https://api.{ambiente}.empresa.com/health";
//
//   using var http = new HttpClient();
//   var resposta = await http.GetStringAsync(url);
//
//   Console.WriteLine($"[{ambiente}] {resposta}");
//
// -----------------------------------------------------------------------

namespace BlogSamples.Scripting.TopLevelStatements;

/// <summary>
/// Classe de referência com documentação dos padrões de top-level statements.
/// O código executável real estaria diretamente em Program.cs de um projeto console.
/// Ver os comentários acima para os exemplos completos.
/// </summary>
public static class ExemploTopLevel
{
    /// <summary>
    /// Demonstra o equivalente de um Program.cs com top-level statements
    /// como método invocável (para fins didáticos neste projeto de exemplos).
    /// </summary>
    public static void ExecutarExemplo(string[] args)
    {
        // Equivalente ao top-level: var ambiente = args.Length > 0 ? args[0] : "desenvolvimento";
        var ambiente = args.Length > 0 ? args[0] : "desenvolvimento";
        Console.WriteLine($"Executando em: {ambiente}");
        Console.WriteLine($"Data/hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }
}
