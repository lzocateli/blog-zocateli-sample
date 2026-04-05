# daemon_service.py — Serviço M2M com Client Credentials
import os
import msal
import requests

# Configuração do daemon/serviço
CLIENT_ID = os.environ["AZURE_CLIENT_ID"]
CLIENT_SECRET = os.environ["AZURE_CLIENT_SECRET"]
TENANT_ID = os.environ["AZURE_TENANT_ID"]
AUTHORITY = f"https://login.microsoftonline.com/{TENANT_ID}"

# Escopo da API alvo — formato: api://<client-id>/.default
API_SCOPE = ["api://api-destino-client-id/.default"]
API_BASE_URL = "https://api.seudominio.com"

def obter_token_m2m():
    """Obtém access token usando Client Credentials (sem usuário)."""
    app = msal.ConfidentialClientApplication(
        CLIENT_ID,
        authority=AUTHORITY,
        client_credential=CLIENT_SECRET,
    )

    # Tenta usar token em cache antes de solicitar novo
    result = app.acquire_token_silent(API_SCOPE, account=None)
    if not result:
        # Solicita novo token via Client Credentials
        result = app.acquire_token_for_client(scopes=API_SCOPE)

    if "access_token" in result:
        return result["access_token"]
    else:
        raise Exception(f"Falha ao obter token: {result.get('error_description')}")

def chamar_api_protegida():
    """Chama a API protegida usando o access token M2M."""
    token = obter_token_m2m()

    # Incluir o token no header Authorization
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json",
    }

    response = requests.get(f"{API_BASE_URL}/api/dados", headers=headers)
    response.raise_for_status()
    return response.json()

if __name__ == "__main__":
    dados = chamar_api_protegida()
    print(f"Dados recebidos da API: {dados}")
