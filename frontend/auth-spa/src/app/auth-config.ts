// src/app/auth-config.ts — Configuração do MSAL para Angular 16+
import { MsalGuardConfiguration, MsalInterceptorConfiguration } from "@azure/msal-angular";
import {
    BrowserCacheLocation,
    InteractionType,
    IPublicClientApplication,
    LogLevel,
    PublicClientApplication,
} from "@azure/msal-browser";

// Configuração do MSAL — NÃO inclui client secret (app pública)
export function MSALInstanceFactory(): IPublicClientApplication {
    return new PublicClientApplication({
        auth: {
            clientId: "seu-client-id-aqui",             // Application (client) ID
            authority: "https://login.microsoftonline.com/seu-tenant-id",
            redirectUri: "http://localhost:4200",         // Redirect URI configurada no portal
            postLogoutRedirectUri: "http://localhost:4200",
        },
        cache: {
            cacheLocation: BrowserCacheLocation.SessionStorage, // sessionStorage para SPAs
            storeAuthStateInCookie: false,
        },
        system: {
            loggerOptions: {
                logLevel: LogLevel.Info,
                loggerCallback: (level, message) => console.log(message),
            },
        },
    });
}

// Configuração do MsalGuard — protege rotas que exigem autenticação
export function MSALGuardConfigFactory(): MsalGuardConfiguration {
    return {
        interactionType: InteractionType.Redirect, // Redirect ou Popup
        authRequest: {
            scopes: ["openid", "profile", "email"],
        },
    };
}

// Configuração do MsalInterceptor — injeta access token automaticamente nas requisições HTTP
export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
    const protectedResources = new Map<string, Array<string>>();
    // Toda requisição para sua API receberá o token automaticamente
    protectedResources.set("https://api.seudominio.com/*", ["api://minha-api/User.Read"]);
    // Microsoft Graph (opcional)
    protectedResources.set("https://graph.microsoft.com/v1.0/*", ["User.Read"]);

    return {
        interactionType: InteractionType.Redirect,
        protectedResourceMap: protectedResources,
    };
}
