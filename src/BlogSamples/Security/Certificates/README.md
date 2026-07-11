# SSL/TLS em .NET

Exemplos de validacao de certificado TLS para cenarios de producao, incluindo:

- validacao padrao do servidor com `HttpClient`
- trust customizado de CA privada com `CustomRootTrust`
- mTLS com certificado de cliente (`.pfx`)

## Uso rapido

```csharp
using BlogSamples.Security.Certificates;

var cliente = HttpsClientesSeguros.CriarClienteComCaPrivada("/etc/ssl/custom/minha-ca.pem");
var resposta = await cliente.GetAsync("https://api.interna.local/health");
resposta.EnsureSuccessStatusCode();
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
