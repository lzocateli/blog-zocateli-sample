# String Analyzer — Exemplo Native AOT CLI Tool

Este projeto demonstra como compilar uma ferramenta CLI em .NET com **Native AOT**. O resultado é um executável nativo standalone, sem dependência de .NET runtime.

## O que faz

A ferramenta analisa um arquivo e exibe:

- Total de caracteres
- Número de caracteres únicos
- Frequência dos N caracteres mais comuns (padrão: top 10)

## Como usar

### Compilar para Native AOT

```bash
# No diretório do projeto

# Restore dependencies
dotnet restore

# Publish como Native AOT (Windows x64)
dotnet publish -c Release -r win-x64 /p:PublishAot=true

# Ou Linux x64
dotnet publish -c Release -r linux-x64 /p:PublishAot=true

# Ou macOS ARM64
dotnet publish -c Release -r osx-arm64 /p:PublishAot=true
```

Executável sai em: `bin/Release/net10.0/<runtime>/publish/StringAnalyzer.Console.exe`

### Executar

```bash
# Uso básico
./StringAnalyzer.Console.exe myfile.txt

# Com opção custom (top 20 caracteres)
./StringAnalyzer.Console.exe myfile.txt --top 20

# Help
./StringAnalyzer.Console.exe --help
```

### Exemplo de Saída

```text
📄 Arquivo: sample.txt
📊 Total de caracteres: 15,432
📈 Caracteres únicos: 87

🔝 Top 10 caracteres mais frequentes:

 1. 'SPACE': 2,841 (18.41%)
 2. 'e': 1,203 (7.79%)
 3. 'a': 987 (6.39%)
 4. 'o': 876 (5.68%)
 5. 'NEWLINE': 145 (0.94%)
 6. 't': 892 (5.78%)
 7. 'i': 645 (4.18%)
 8. 'n': 612 (3.97%)
 9. 's': 534 (3.46%)
10. 'r': 498 (3.23%)

✅ Análise concluída com sucesso.
```

## Por que este projeto é AOT-friendly?

1. **Parsing direto de argumentos** — usa apenas APIs da biblioteca padrão, sem reflection
2. **Sem reflection dinâmica** — todo acesso a tipos é resolvido em compile-time
3. **I/O simples** — `File.ReadAllText()` e `StreamReader` funcionam perfeitamente em AOT
4. **Sem source generators necessários** — não há serialização ou dependency injection complexa
5. **Código procedural** — sem patterns avançados que dependam de reflection

## Comparação de Tamanho

| Build                     | Tamanho                                  |
| ------------------------- | ---------------------------------------- |
| **JIT (DLL + Runtime)**   | ~100 MB (dependência de .NET runtime)    |
| **Native AOT Executable** | ~8–12 MB                                 |

O executável AOT é standalone e não requer .NET instalado.

## Possíveis Extensões

- Adicionar suporte para múltiplos arquivos
- Gráficos ASCII de distribuição de frequência
- Export para CSV/JSON
- Análise de padrões (digrams, trigrams)

## Avisos AOT

Se receber warnings durante compilação, verifique:

```bash
dotnet build /p:PublishAot=true -v normal
```

Warnings típicos indicam uso de reflection. Resolva com:

- `[DynamicallyAccessedMembers]` attribute
- `[TrimmerRootAssembly]` assembly-level
- Refatore o código para evitar reflection

## Referências

- [Microsoft Docs — Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Trimming .NET Applications](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained)
