using System;
using System.IO;
using System.Linq;
using StringAnalyzer.Console;

var cmdArgs = Environment.GetCommandLineArgs();

if (cmdArgs.Length < 2 || cmdArgs[1] == "--help" || cmdArgs[1] == "-h")
{
    Console.WriteLine("String Analyzer - Analisa frequência de caracteres em arquivos");
    Console.WriteLine("\nUso: StringAnalyzer.Console.exe <arquivo> [--top N]");
    Console.WriteLine("\nOpções:");
    Console.WriteLine("  <arquivo>     Caminho do arquivo para análise");
    Console.WriteLine("  --top N       Número de caracteres mais frequentes (padrão: 10)");
    Console.WriteLine("  --help, -h    Mostra esta ajuda");
    return cmdArgs.Length < 2 ? 1 : 0;
}

string filePath = cmdArgs[1];
int top = 10;

// Parse --top option
for (int i = 2; i < cmdArgs.Length; i++)
{
    if (cmdArgs[i] == "--top" && i + 1 < cmdArgs.Length)
    {
        if (!int.TryParse(cmdArgs[i + 1], out top))
        {
            Console.Error.WriteLine("❌ Erro: --top deve receber um número inteiro.");
            return 1;
        }
    }
}

// Validate file
if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"❌ Erro: Arquivo '{filePath}' não encontrado.");
    return 1;
}

try
{
    var analyzer = new AnalyzerService();
    var result = analyzer.Analyze(filePath);
    
    // Display results
    var fileName = Path.GetFileName(filePath);
    Console.WriteLine($"\n📄 Arquivo: {fileName}");
    Console.WriteLine($"📊 Total de caracteres: {result.TotalChars:N0}");
    Console.WriteLine($"📈 Caracteres únicos: {result.UniqueChars}");
    Console.WriteLine($"\n🔝 Top {Math.Min(top, result.TopFrequent.Count)} caracteres mais frequentes:\n");
    
    int rank = 1;
    foreach (var kvp in result.TopFrequent.Take(top))
    {
        char ch = kvp.Key;
        int count = kvp.Value;
        double percentage = (count / (double)result.TotalChars) * 100;
        string displayChar = char.IsWhiteSpace(ch) switch
        {
            true when ch == ' ' => "SPACE",
            true when ch == '\n' => "NEWLINE",
            true when ch == '\t' => "TAB",
            true => "WHITESPACE",
            false => ch.ToString()
        };
        
        Console.WriteLine($"{rank:2}. '{displayChar}': {count:N0} ({percentage:F2}%)");
        rank++;
    }
    
    Console.WriteLine("\n✅ Análise concluída com sucesso.\n");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"❌ Erro ao processar arquivo: {ex.Message}");
    return 1;
}
