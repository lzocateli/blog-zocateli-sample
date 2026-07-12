// -----------------------------------------------------------------------
// Artigo: Certificados SSL/TLS: Como Funcionam e Como Validar
// URL: https://zocate.li/posts/2026/certificado-ssl-tls/
// Demonstra validacao de certificado e mTLS com HttpClient em producao.
// -----------------------------------------------------------------------

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace BlogSamples.Security.Certificates;

public static class CertificadoTlsValidator
{
    public static SocketsHttpHandler CriarHandlerPadraoSeguro()
    {
        return new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.Online
            }
        };
    }

    public static SocketsHttpHandler CriarHandlerComCaPrivada(string caminhoCaRaiz)
    {
        var caRaiz = X509CertificateLoader.LoadCertificateFromFile(caminhoCaRaiz);

        return new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.Online,
                RemoteCertificateValidationCallback = (_, certificadoRemoto, cadeiaRemota, errosSsl) =>
                    ValidarServidor(certificadoRemoto, cadeiaRemota, errosSsl, caRaiz)
            }
        };
    }

    public static SocketsHttpHandler CriarHandlerComMtls(
        string caminhoCaRaiz,
        string caminhoPfxCliente,
        string senhaPfxCliente)
    {
        var certificadoCliente = X509CertificateLoader.LoadPkcs12FromFile(
            caminhoPfxCliente,
            senhaPfxCliente.AsSpan(),
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

        var caRaiz = X509CertificateLoader.LoadCertificateFromFile(caminhoCaRaiz);

        return new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.Online,
                ClientCertificates = new X509CertificateCollection { certificadoCliente },
                RemoteCertificateValidationCallback = (_, certificadoRemoto, cadeiaRemota, errosSsl) =>
                    ValidarServidor(certificadoRemoto, cadeiaRemota, errosSsl, caRaiz)
            }
        };
    }

    public static bool ValidarServidor(
        X509Certificate? certificadoRemoto,
        X509Chain? cadeiaRemota,
        SslPolicyErrors errosSsl,
        X509Certificate2? caRaizPersonalizada = null)
    {
        if (certificadoRemoto is null)
            return false;

        if (errosSsl.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable) ||
            errosSsl.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            return false;
        }

        using var certificadoServidor = certificadoRemoto as X509Certificate2
            ?? X509CertificateLoader.LoadCertificate(certificadoRemoto.Export(X509ContentType.Cert));

        using var cadeia = cadeiaRemota ?? new X509Chain();
        var politica = cadeia.ChainPolicy;

        politica.RevocationMode = X509RevocationMode.Online;
        politica.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        politica.VerificationFlags = X509VerificationFlags.NoFlag;
        politica.DisableCertificateDownloads = false;
        politica.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);

        if (caRaizPersonalizada is not null)
        {
            politica.TrustMode = X509ChainTrustMode.CustomRootTrust;
            politica.CustomTrustStore.Clear();
            politica.CustomTrustStore.Add(caRaizPersonalizada);
        }

        var cadeiaValida = cadeia.Build(certificadoServidor);

        return cadeiaValida;
    }
}
