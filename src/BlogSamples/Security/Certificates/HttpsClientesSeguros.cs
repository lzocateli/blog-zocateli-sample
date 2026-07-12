// -----------------------------------------------------------------------
// Artigo: Certificados SSL/TLS: Como Funcionam e Como Validar
// URL: https://zocate.li/posts/2026/certificado-ssl-tls/
// Demonstra validacao de certificado e mTLS com HttpClient em producao.
// -----------------------------------------------------------------------

namespace BlogSamples.Security.Certificates;

public static class HttpsClientesSeguros
{
    public static HttpClient CriarClientePadrao()
    {
        var handler = CertificadoTlsValidator.CriarHandlerPadraoSeguro();

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public static HttpClient CriarClienteComCaPrivada(string caminhoCaRaiz)
    {
        var handler = CertificadoTlsValidator.CriarHandlerComCaPrivada(caminhoCaRaiz);

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public static HttpClient CriarClienteComMtls(
        string caminhoCaRaiz,
        string caminhoPfxCliente,
        string senhaPfxCliente)
    {
        var handler = CertificadoTlsValidator.CriarHandlerComMtls(
            caminhoCaRaiz,
            caminhoPfxCliente,
            senhaPfxCliente);

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }
}
