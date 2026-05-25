#!/usr/bin/env dotnet-script
// -----------------------------------------------------------------------
// Artigo: C# como Script no .NET: Como Usar e Quando Vale a Pena
// URL: https://zocate.li/posts/2026/executar-csharp-como-script-dotnet/
// Cenário completo: importar CSV de produtos e enviar para API REST.
//
// Pré-requisitos:
//   - Variável de ambiente API_URL configurada
//   - Arquivo produtos.csv no mesmo diretório (ver modelo abaixo)
//
// Execução:
//   $env:API_URL = "https://localhost:7063"
//   dotnet script importar-csv-api.csx
//   dotnet script importar-csv-api.csx -- --dry-run   (sem enviar para API)
//
// Modelo do CSV (produtos.csv):
//   Nome,Preco,Estoque,Categoria
//   Notebook Dell XPS,8999.90,15,Informatica
//   Mouse Logitech MX,399.90,50,Perifericos
//   Teclado Keychron K8,649.90,30,Perifericos
// -----------------------------------------------------------------------

#r "nuget: CsvHelper, 33.0.1"

using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

// --- Configuração ---

var dryRun = Args.Contains("--dry-run");
var csvPath = "produtos.csv";

var apiUrl = Environment.GetEnvironmentVariable("API_URL")
    ?? "http://localhost:5101"; // fallback para desenvolvimento local

Console.WriteLine("=== Importador de Produtos via CSV ===");
Console.WriteLine($"API URL : {apiUrl}");
Console.WriteLine($"Dry-run : {dryRun}");
Console.WriteLine($"CSV     : {csvPath}");
Console.WriteLine();

if (!File.Exists(csvPath))
{
    Console.Error.WriteLine($"✗ Arquivo não encontrado: {csvPath}");
    Console.Error.WriteLine("  Crie o arquivo com as colunas: Nome,Preco,Estoque,Categoria");
    Environment.Exit(1);
}

// --- Leitura do CSV ---

var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    MissingFieldFound = null, // ignora colunas ausentes
};

List<ProdutoImportacao> produtos;
using (var reader = new StreamReader(csvPath))
using (var csv = new CsvReader(reader, config))
{
    produtos = csv.GetRecords<ProdutoImportacao>().ToList();
}

Console.WriteLine($"Registros lidos: {produtos.Count}");
Console.WriteLine();

if (produtos.Count == 0)
{
    Console.WriteLine("Nenhum registro encontrado no CSV. Encerrando.");
    return;
}

// --- Envio para a API ---

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n⚠ Cancelamento solicitado...");
};

var sucesso = 0;
var falha = 0;

using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

foreach (var produto in produtos)
{
    if (cts.Token.IsCancellationRequested) break;

    try
    {
        if (dryRun)
        {
            Console.WriteLine($"  [dry-run] {produto.Nome} — R$ {produto.Preco:F2} ({produto.Estoque} un)");
            sucesso++;
            continue;
        }

        // Retry simples: até 3 tentativas com backoff
        HttpResponseMessage? resposta = null;
        for (int tentativa = 1; tentativa <= 3; tentativa++)
        {
            try
            {
                resposta = await http.PostAsJsonAsync("/produtos", produto, cts.Token);
                if (resposta.IsSuccessStatusCode) break;

                Console.WriteLine($"  ⚠ {produto.Nome} — tentativa {tentativa} falhou: HTTP {(int)resposta.StatusCode}");
                await Task.Delay(TimeSpan.FromSeconds(tentativa), cts.Token);
            }
            catch (HttpRequestException ex) when (tentativa < 3)
            {
                Console.WriteLine($"  ⚠ {produto.Nome} — tentativa {tentativa} erro: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(tentativa), cts.Token);
            }
        }

        if (resposta is not null && resposta.IsSuccessStatusCode)
        {
            var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cts.Token);
            var id = criado.TryGetProperty("id", out var idProp) ? idProp.ToString() : "?";
            Console.WriteLine($"  ✓ {produto.Nome} — id: {id}");
            sucesso++;
        }
        else
        {
            Console.WriteLine($"  ✗ {produto.Nome} — falhou após 3 tentativas");
            falha++;
        }
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ✗ {produto.Nome} — erro inesperado: {ex.Message}");
        falha++;
    }
}

// --- Sumário ---

Console.WriteLine();
Console.WriteLine("=== Resultado ===");
Console.WriteLine($"  Sucesso : {sucesso}");
Console.WriteLine($"  Falha   : {falha}");
Console.WriteLine($"  Total   : {produtos.Count}");

if (falha > 0) Environment.Exit(1);

// --- Modelo de dados ---

public record ProdutoImportacao(
    string Nome,
    decimal Preco,
    int Estoque,
    string Categoria
);
