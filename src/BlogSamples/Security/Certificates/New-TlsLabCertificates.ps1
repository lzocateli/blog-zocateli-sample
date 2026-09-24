<#
.SYNOPSIS
Gera certificados locais para demonstrar TLS Termination, Re-encryption, Passthrough e mTLS.

.DESCRIPTION
Cria uma CA raiz de laboratorio e tres certificados assinados por ela:
um certificado de servidor para Load Balancer/Proxy Reverso, um certificado de servidor
para backend HTTPS e um certificado de cliente para mTLS.

Os arquivos gerados contem chaves privadas e devem ser usados apenas em laboratorio.
Nao versionar a pasta de saida.

.PARAMETER OutputPath
Diretorio onde os certificados serao gravados.

.PARAMETER Password
Senha dos arquivos PFX gerados. Valor padrao: changeit.

.PARAMETER Force
Sobrescreve arquivos existentes no diretorio de saida.

.EXAMPLE
./New-TlsLabCertificates.ps1

.EXAMPLE
./New-TlsLabCertificates.ps1 -OutputPath ./certs -Password changeit -Force
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath = (Join-Path $PSScriptRoot 'certs'),

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$Password = 'changeit',

    [Parameter()]
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-SerialNumber {
    $serial = [byte[]]::new(16)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($serial)
    return $serial
}

function Add-CommonExtensions {
    param(
        [Parameter(Mandatory)]
        [System.Security.Cryptography.X509Certificates.CertificateRequest]$Request,

        [Parameter(Mandatory)]
        [bool]$IsCa
    )

    $basicConstraints = [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
        $IsCa,
        $IsCa,
        0,
        $true)
    $Request.CertificateExtensions.Add($basicConstraints)

    $subjectKeyIdentifier = [System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]::new(
        $Request.PublicKey,
        $false)
    $Request.CertificateExtensions.Add($subjectKeyIdentifier)
}

function New-LabRootCa {
    $rsa = [System.Security.Cryptography.RSA]::Create(4096)
    $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        'CN=Zocateli TLS Lab Root CA, O=Zocateli Lab',
        $rsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

    Add-CommonExtensions -Request $request -IsCa $true

    $keyUsage = [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
        [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyCertSign -bor
        [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::CrlSign,
        $true)
    $request.CertificateExtensions.Add($keyUsage)

    $certificate = $request.CreateSelfSigned(
        [datetimeoffset]::UtcNow.AddDays(-1),
        [datetimeoffset]::UtcNow.AddYears(5))

    return [pscustomobject]@{
        Certificate = $certificate
        Key = $rsa
    }
}

function New-LabCertificate {
    param(
        [Parameter(Mandatory)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Issuer,

        [Parameter(Mandatory)]
        [string]$Subject,

        [Parameter()]
        [string[]]$DnsNames = @(),

        [Parameter(Mandatory)]
        [System.Security.Cryptography.Oid[]]$EnhancedKeyUsages
    )

    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        $Subject,
        $rsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

    Add-CommonExtensions -Request $request -IsCa $false

    $request.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature -bor
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment,
            $true))

    $enhancedKeyUsageCollection = [System.Security.Cryptography.OidCollection]::new()
    foreach ($enhancedKeyUsage in $EnhancedKeyUsages) {
        [void]$enhancedKeyUsageCollection.Add($enhancedKeyUsage)
    }

    $request.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new(
            $enhancedKeyUsageCollection,
            $false))

    if ($DnsNames.Length -gt 0) {
        $sanBuilder = [System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
        foreach ($dnsName in $DnsNames) {
            $sanBuilder.AddDnsName($dnsName)
        }
        $sanBuilder.AddIpAddress([System.Net.IPAddress]::Parse('127.0.0.1'))
        $request.CertificateExtensions.Add($sanBuilder.Build())
    }

    $signed = $request.Create(
        $Issuer,
        [datetimeoffset]::UtcNow.AddDays(-1),
        [datetimeoffset]::UtcNow.AddYears(2),
        (New-SerialNumber))

    $certificate = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::CopyWithPrivateKey(
        $signed,
        $rsa)

    return [pscustomobject]@{
        Certificate = $certificate
        Key = $rsa
    }
}

function Export-LabCertificate {
    param(
        [Parameter(Mandatory)]
        [pscustomobject]$CertInfo,

        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$TargetPath,

        [Parameter(Mandatory)]
        [string]$PfxPassword
    )

    [System.IO.File]::WriteAllText(
        (Join-Path $TargetPath "$Name.crt"),
        $CertInfo.Certificate.ExportCertificatePem())

    [System.IO.File]::WriteAllText(
        (Join-Path $TargetPath "$Name.key"),
        $CertInfo.Key.ExportPkcs8PrivateKeyPem())

    [System.IO.File]::WriteAllBytes(
        (Join-Path $TargetPath "$Name.pfx"),
        $CertInfo.Certificate.Export(
            [System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12,
            $PfxPassword))
}

if ((Test-Path $OutputPath) -and -not $Force) {
    $existingFiles = Get-ChildItem -Path $OutputPath -File -ErrorAction SilentlyContinue
    if ($existingFiles) {
        throw "O diretorio '$OutputPath' ja contem arquivos. Use -Force para sobrescrever."
    }
}

if ($PSCmdlet.ShouldProcess($OutputPath, 'Gerar certificados TLS de laboratorio')) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

    $serverAuth = [System.Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.1')
    $clientAuth = [System.Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.2')

    $rootCa = New-LabRootCa
    $lbProxy = New-LabCertificate `
        -Issuer $rootCa.Certificate `
        -Subject 'CN=api2.interno.empresa.com, O=Zocateli Lab' `
        -DnsNames @('api2.interno.empresa.com', 'api.minhaempresa.com', 'localhost') `
        -EnhancedKeyUsages @($serverAuth)

    $backend = New-LabCertificate `
        -Issuer $rootCa.Certificate `
        -Subject 'CN=backend.interno.local, O=Zocateli Lab' `
        -DnsNames @('backend.interno.local', 'api2-backend.interno.local', 'localhost') `
        -EnhancedKeyUsages @($serverAuth)

    $client = New-LabCertificate `
        -Issuer $rootCa.Certificate `
        -Subject 'CN=api1-cliente, O=Zocateli Lab' `
        -DnsNames @() `
        -EnhancedKeyUsages @($clientAuth)

    Export-LabCertificate -CertInfo $rootCa -Name 'root-ca' -TargetPath $OutputPath -PfxPassword $Password
    Export-LabCertificate -CertInfo $lbProxy -Name 'lb-proxy' -TargetPath $OutputPath -PfxPassword $Password
    Export-LabCertificate -CertInfo $backend -Name 'backend-api' -TargetPath $OutputPath -PfxPassword $Password
    Export-LabCertificate -CertInfo $client -Name 'client-api1' -TargetPath $OutputPath -PfxPassword $Password

    Write-Host "Certificados gerados em: $OutputPath"
    Write-Host 'Use root-ca.crt como CA confiavel nos clientes.'
    Write-Host 'Use lb-proxy.pfx/crt/key no Load Balancer ou Reverse Proxy.'
    Write-Host 'Use backend-api.pfx/crt/key no backend quando houver Re-encryption ou Passthrough.'
    Write-Host 'Use client-api1.pfx ou client-api1.crt/key para demonstrar mTLS.'
}