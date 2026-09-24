# SSL/TLS em .NET

Exemplos de validacao de certificado TLS para cenarios de producao e laboratorio, incluindo:

- validacao padrao do servidor com `HttpClient`
- trust customizado de CA privada com `CustomRootTrust`
- mTLS com certificado de cliente (`.pfx`)
- TLS Termination em Load Balancer ou Reverse Proxy
- TLS Re-encryption entre proxy e backend HTTPS
- TLS Passthrough ate a API backend

## Gerar certificados de laboratorio

Execute o script abaixo para gerar uma CA raiz local e certificados para cada papel do artigo:

```powershell
./New-TlsLabCertificates.ps1 -OutputPath ./certs -Password changeit -Force
```

Arquivos gerados:

| Arquivo | Uso |
|---|---|
| `root-ca.crt` | CA confiavel para .NET, Python, NGINX ou sistema operacional |
| `root-ca.pfx` / `root-ca.key` | Chave da CA apenas para laboratorio e reemissao local |
| `lb-proxy.pfx` / `lb-proxy.crt` / `lb-proxy.key` | Certificado de servidor apresentado pelo Load Balancer/Proxy |
| `backend-api.pfx` / `backend-api.crt` / `backend-api.key` | Certificado de servidor da API backend em Re-encryption ou Passthrough |
| `client-api1.pfx` / `client-api1.crt` / `client-api1.key` | Certificado de cliente para mTLS na borda |

> Os arquivos em `certs/` contem chaves privadas de laboratorio e estao ignorados pelo Git.

## Uso rapido

```csharp
using BlogSamples.Security.Certificates;

var cliente = HttpsClientesSeguros.CriarClienteComCaPrivada("/etc/ssl/custom/minha-ca.pem");
var resposta = await cliente.GetAsync("https://api.interna.local/health");
resposta.EnsureSuccessStatusCode();
```

## Cenarios com Load Balancer e Proxy Reverso

```csharp
using BlogSamples.Security.Certificates;

var clienteTermination = CenariosTlsComProxyReverso
    .CriarClienteParaTlsTerminationOuReencryption("./certs/root-ca.crt");

var resposta = await clienteTermination.GetAsync(
    CenariosTlsComProxyReverso.EndpointApiViaBorda);

resposta.EnsureSuccessStatusCode();
```

No TLS Termination e no TLS Re-encryption, a API cliente valida o certificado da borda (`lb-proxy`). No Passthrough, a API cliente valida o certificado do backend (`backend-api`).

```csharp
foreach (var cenario in CenariosTlsComProxyReverso.ListarCenarios())
{
    Console.WriteLine($"{cenario.Nome}: {cenario.Fluxo}");
}
```

## mTLS

```csharp
var clienteMtls = HttpsClientesSeguros.CriarClienteComMtls(
    caminhoCaRaiz: "/etc/ssl/custom/minha-ca.pem",
    caminhoPfxCliente: "/run/secrets/cliente.pfx",
    senhaPfxCliente: Environment.GetEnvironmentVariable("CLIENT_CERT_PASSWORD") ?? string.Empty);
```

## Container (Debian/Ubuntu)

```dockerfile
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY certs/minha-ca.crt /usr/local/share/ca-certificates/minha-ca.crt
RUN update-ca-certificates
```

Recomendacao: montar certificado e chave de cliente como secret em tmpfs (por exemplo `/run/secrets`) em vez de variaveis de ambiente.
