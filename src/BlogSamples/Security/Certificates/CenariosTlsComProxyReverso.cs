// -----------------------------------------------------------------------
// Artigo: Certificados SSL/TLS: Como Funcionam e Como Validar
// URL: https://zocate.li/posts/2026/certificado-ssl-tls/
// Demonstra TLS Termination, Re-encryption, Passthrough e mTLS na borda.
// -----------------------------------------------------------------------

namespace BlogSamples.Security.Certificates;

public sealed record CenarioTlsProxy(
    string Nome,
    string Fluxo,
    string CertificadoValidadoPelaApiCliente,
    string CertificadoValidadoPeloProxy,
    string QuandoUsar);

public static class CenariosTlsComProxyReverso
{
    public static readonly Uri EndpointApiViaBorda = new("https://api2.interno.empresa.com/health");

    public static IReadOnlyList<CenarioTlsProxy> ListarCenarios()
    {
        return
        [
            new CenarioTlsProxy(
                Nome: "TLS Termination",
                Fluxo: "API 1 -> HTTPS -> Load Balancer/Proxy -> HTTP -> Container API 2",
                CertificadoValidadoPelaApiCliente: "Certificado do Load Balancer ou Proxy Reverso",
                CertificadoValidadoPeloProxy: "Nenhum certificado de backend, pois o trecho interno usa HTTP",
                QuandoUsar: "Rede interna controlada, com segmentacao, firewall e observabilidade"),

            new CenarioTlsProxy(
                Nome: "TLS Re-encryption",
                Fluxo: "API 1 -> HTTPS -> Load Balancer/Proxy -> HTTPS -> Container API 2",
                CertificadoValidadoPelaApiCliente: "Certificado do Load Balancer ou Proxy Reverso",
                CertificadoValidadoPeloProxy: "Certificado do backend HTTPS, validado com a CA configurada no proxy",
                QuandoUsar: "Ambientes regulados, Kubernetes multi-tenant ou rede interna compartilhada"),

            new CenarioTlsProxy(
                Nome: "TLS Passthrough",
                Fluxo: "API 1 -> HTTPS -> Load Balancer TCP -> Container API 2",
                CertificadoValidadoPelaApiCliente: "Certificado da propria API backend",
                CertificadoValidadoPeloProxy: "Nenhum, pois o proxy nao encerra TLS",
                QuandoUsar: "Quando o proxy nao deve inspecionar HTTP nem encerrar a sessao TLS"),

            new CenarioTlsProxy(
                Nome: "mTLS na borda",
                Fluxo: "API 1 com certificado de cliente -> HTTPS -> Load Balancer/Proxy",
                CertificadoValidadoPelaApiCliente: "Certificado do Load Balancer ou Proxy Reverso",
                CertificadoValidadoPeloProxy: "Certificado de cliente da API 1, validado pela CA de clientes confiaveis",
                QuandoUsar: "Integracao service-to-service com identidade forte na borda")
        ];
    }

    public static HttpClient CriarClienteParaTlsTerminationOuReencryption(string caminhoCaDaBorda)
    {
        return HttpsClientesSeguros.CriarClienteComCaPrivada(caminhoCaDaBorda);
    }

    public static HttpClient CriarClienteParaTlsPassthrough(string caminhoCaDoBackend)
    {
        return HttpsClientesSeguros.CriarClienteComCaPrivada(caminhoCaDoBackend);
    }

    public static HttpClient CriarClienteMtlsNaBorda(
        string caminhoCaDaBorda,
        string caminhoPfxCliente,
        string senhaPfxCliente)
    {
        return HttpsClientesSeguros.CriarClienteComMtls(
            caminhoCaDaBorda,
            caminhoPfxCliente,
            senhaPfxCliente);
    }
}