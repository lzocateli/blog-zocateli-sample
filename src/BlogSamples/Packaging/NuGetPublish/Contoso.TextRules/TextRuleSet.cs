// -----------------------------------------------------------------------
// Artigo: Como Publicar uma Biblioteca .NET no NuGet.org
// URL: https://zocate.li/posts/2026/publicar-biblioteca-dotnet-nuget/
// Biblioteca de exemplo para validar regras de texto e demonstrar o pacote NuGet.
// -----------------------------------------------------------------------

namespace BlogSamples.Packaging.NuGetPublish;

public sealed class TextRuleSet
{
    public bool IsValidEmail(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Contains('@')
            && value.Contains('.')
            && value.IndexOf('@', StringComparison.Ordinal) > 0
            && value.LastIndexOf('.', value.Length - 1) > value.IndexOf('@', StringComparison.Ordinal);
    }

    public string NormalizeWhitespace(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public bool ContainsAny(string value, params string[] tokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = NormalizeWhitespace(value).Trim();
        var values = tokens.Where(t => !string.IsNullOrWhiteSpace(t));

        return values.Any(token => normalized.Contains(token.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
