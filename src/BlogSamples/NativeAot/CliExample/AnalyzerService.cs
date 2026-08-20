namespace StringAnalyzer.Console;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Serviço para análise de frequência de caracteres em arquivos.
/// Implementado para ser AOT-compatible: sem reflection dinâmica.
/// </summary>
public class AnalyzerService
{
    public AnalysisResult Analyze(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Arquivo não encontrado: {filePath}");

        var frequencyMap = new Dictionary<char, int>();
        int totalChars = 0;

        // Read file and count character frequency
        using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
        {
            const int bufferSize = 8192;
            var buffer = new char[bufferSize];
            int charsRead;

            while ((charsRead = reader.Read(buffer, 0, bufferSize)) > 0)
            {
                for (int i = 0; i < charsRead; i++)
                {
                    char ch = buffer[i];
                    
                    if (frequencyMap.ContainsKey(ch))
                        frequencyMap[ch]++;
                    else
                        frequencyMap[ch] = 1;
                    
                    totalChars++;
                }
            }
        }

        // Sort by frequency (descending)
        var sorted = frequencyMap
            .OrderByDescending(kvp => kvp.Value)
            .ToList();

        return new AnalysisResult
        {
            TotalChars = totalChars,
            UniqueChars = frequencyMap.Count,
            TopFrequent = sorted
        };
    }
}

/// <summary>
/// Resultado da análise de frequência.
/// </summary>
public class AnalysisResult
{
    public int TotalChars { get; set; }
    public int UniqueChars { get; set; }
    public List<KeyValuePair<char, int>> TopFrequent { get; set; } = new();
}
