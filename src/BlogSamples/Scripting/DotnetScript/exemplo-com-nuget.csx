#!/usr/bin/env dotnet-script
// -----------------------------------------------------------------------
// Artigo: C# como Script no .NET: Como Usar e Quando Vale a Pena
// URL: https://zocate.li/posts/2026/executar-csharp-como-script-dotnet/
// Script .csx com referências NuGet inline — sem csproj, sem dotnet add package.
//
// Execução:
//   dotnet script exemplo-com-nuget.csx
//
// Na primeira execução, o dotnet-script baixa os pacotes automaticamente.
// -----------------------------------------------------------------------

// Referências NuGet inline — sintaxe: #r "nuget: NomePacote, Versão"
#r "nuget: Humanizer.Core, 2.14.1"
#r "nuget: Spectre.Console, 0.49.1"

using Humanizer;
using Spectre.Console;

AnsiConsole.Write(new Rule("[bold cyan]Exemplo: NuGet inline com dotnet-script[/]"));
AnsiConsole.WriteLine();

// --- Humanizer: formatação legível de números, datas e strings ---

// Tamanhos em bytes → leitura humana
var tamanhos = new long[] { 512, 1_048_576, 2_147_483_648 };
foreach (var bytes in tamanhos)
{
    AnsiConsole.MarkupLine($"  [grey]{bytes,15:N0}[/] bytes → [green]{bytes.Bytes().Humanize()}[/]");
}

AnsiConsole.WriteLine();

// Datas relativas
var datas = new[]
{
    DateTime.Now.AddMinutes(-5),
    DateTime.Now.AddHours(-3),
    DateTime.Now.AddDays(-7),
    DateTime.Now.AddMonths(-2),
};
foreach (var data in datas)
{
    AnsiConsole.MarkupLine($"  [grey]{data:yyyy-MM-dd HH:mm}[/] → [yellow]{data.Humanize()}[/]");
}

AnsiConsole.WriteLine();

// Pluralização e ordinal
for (int i = 1; i <= 5; i++)
{
    AnsiConsole.MarkupLine($"  {i.ToOrdinalWords()} item");
}

AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[dim]Fim do script[/]"));
