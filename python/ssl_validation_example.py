"""Exemplos de validacao SSL/TLS em Python para producao.

Artigo: Certificados SSL/TLS: Como Funcionam e Como Validar
URL: https://zocate.li/posts/2026/certificado-ssl-tls/
"""

from __future__ import annotations

import os
import ssl
from pathlib import Path

import requests


def get_env_path(env_name: str) -> Path:
    value = os.getenv(env_name)
    if not value:
        raise ValueError(f"Variavel {env_name} nao definida.")
    return Path(value)


def requisicao_https_com_ca_privada(url: str, ca_bundle: Path) -> requests.Response:
    """Executa requisicao HTTPS validando o servidor com CA customizada."""
    response = requests.get(url, verify=str(ca_bundle), timeout=10)
    response.raise_for_status()
    return response


def criar_contexto_ssl(ca_bundle: Path) -> ssl.SSLContext:
    """Cria SSLContext estrito: valida cadeia e hostname."""
    context = ssl.create_default_context(purpose=ssl.Purpose.SERVER_AUTH)
    context.verify_mode = ssl.CERT_REQUIRED
    context.check_hostname = True
    context.minimum_version = ssl.TLSVersion.TLSv1_2
    context.load_verify_locations(cafile=str(ca_bundle))
    return context


def requisicao_mtls_requests(
    url: str,
    ca_bundle: Path,
    client_cert: Path,
    client_key: Path,
) -> requests.Response:
    """Executa mTLS no requests com par cert/key do cliente."""
    response = requests.get(
        url,
        verify=str(ca_bundle),
        cert=(str(client_cert), str(client_key)),
        timeout=10,
    )
    response.raise_for_status()
    return response


def exemplo_execucao() -> None:
    url = os.getenv("TLS_TEST_URL", "https://localhost:8443/health")

    ca_bundle = get_env_path("TLS_CA_BUNDLE")
    client_cert = get_env_path("TLS_CLIENT_CERT")
    client_key = get_env_path("TLS_CLIENT_KEY")

    response = requisicao_https_com_ca_privada(url, ca_bundle)
    print("Resposta com CA customizada:", response.status_code)

    contexto = criar_contexto_ssl(ca_bundle)
    print("SSLContext pronto para uso. check_hostname=", contexto.check_hostname)

    response_mtls = requisicao_mtls_requests(url, ca_bundle, client_cert, client_key)
    print("Resposta mTLS:", response_mtls.status_code)


if __name__ == "__main__":
    exemplo_execucao()
