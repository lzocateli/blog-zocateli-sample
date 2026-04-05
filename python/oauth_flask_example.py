# app.py — Aplicação Flask com autenticação via Azure Entra ID
import os
from flask import Flask, redirect, url_for, session, request
import msal

app = Flask(__name__)
app.secret_key = os.environ.get("FLASK_SECRET_KEY", "dev-secret-key")

# Configuração — Client Secret armazenado em variável de ambiente
CLIENT_ID = os.environ["AZURE_CLIENT_ID"]
CLIENT_SECRET = os.environ["AZURE_CLIENT_SECRET"]  # Seguro no servidor
AUTHORITY = f"https://login.microsoftonline.com/{os.environ['AZURE_TENANT_ID']}"
REDIRECT_URI = "https://app.seudominio.com/auth/callback"
SCOPE = ["User.Read"]  # Escopos do Microsoft Graph

# Criar instância confidencial do MSAL
def criar_msal_app():
    return msal.ConfidentialClientApplication(
        CLIENT_ID,
        authority=AUTHORITY,
        client_credential=CLIENT_SECRET,  # Secret usado aqui (servidor seguro)
    )

@app.route("/login")
def login():
    """Inicia o fluxo de autenticação OAuth 2.0 Authorization Code."""
    msal_app = criar_msal_app()
    # Gerar URL de autorização
    auth_url = msal_app.get_authorization_request_url(
        scopes=SCOPE,
        redirect_uri=REDIRECT_URI,
    )
    return redirect(auth_url)

@app.route("/auth/callback")
def callback():
    """Callback do Azure Entra ID após autenticação."""
    msal_app = criar_msal_app()
    code = request.args.get("code")

    # Trocar authorization code por tokens
    result = msal_app.acquire_token_by_authorization_code(
        code,
        scopes=SCOPE,
        redirect_uri=REDIRECT_URI,
    )

    if "access_token" in result:
        session["user"] = result.get("id_token_claims")
        session["access_token"] = result["access_token"]
        return redirect(url_for("index"))
    else:
        return f"Erro na autenticação: {result.get('error_description')}", 401

@app.route("/")
def index():
    """Página principal — requer autenticação."""
    user = session.get("user")
    if not user:
        return redirect(url_for("login"))
    return f"Olá, {user.get('name')}! Você está autenticado."

if __name__ == "__main__":
    app.run(debug=True, port=5000)
