# Contoso.TextRules

Biblioteca leve para validar e normalizar textos reutilizáveis em aplicações .NET.

## Exemplo de uso

```csharp
using BlogSamples.Packaging.NuGetPublish;

var rules = new TextRuleSet();

var nome = rules.NormalizeWhitespace("  hello   world  ");
var emailValido = rules.IsValidEmail("contato@exemplo.com");
var temToken = rules.ContainsAny("validação de texto", "texto", "regras");
```

## Publicação

Este pacote serve como exemplo prático para o artigo "Como Publicar uma Biblioteca .NET no NuGet.org".
