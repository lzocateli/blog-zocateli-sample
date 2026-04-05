// src/app/auth/csrf.interceptor.ts — Interceptor que envia o token CSRF ao BFF
import { HttpInterceptorFn } from "@angular/common/http";

/**
 * Interceptor funcional (Angular 16+) que lê o cookie XSRF-TOKEN
 * e o envia no header X-XSRF-TOKEN em requisições mutáveis (POST, PUT, DELETE).
 *
 * O Angular HttpClient com withXsrfConfiguration já faz isso automaticamente
 * para requisições same-origin, mas este interceptor garante o comportamento
 * explícito e funciona com configurações customizadas.
 */
export const csrfInterceptor: HttpInterceptorFn = (req, next) => {
    // Apenas requisições que modificam dados precisam do token CSRF
    const metodosMutaveis = ["POST", "PUT", "DELETE", "PATCH"];

    if (metodosMutaveis.includes(req.method.toUpperCase())) {
        // Ler o cookie XSRF-TOKEN (definido pelo BFF com HttpOnly=false)
        const csrfToken = obterCookie("XSRF-TOKEN");

        if (csrfToken) {
            req = req.clone({
                setHeaders: { "X-XSRF-TOKEN": csrfToken },
            });
        }
    }

    return next(req);
};

/** Utilitário para ler um cookie pelo nome. */
function obterCookie(nome: string): string | null {
    const match = document.cookie.match(new RegExp(`(^| )${nome}=([^;]+)`));
    return match ? decodeURIComponent(match[2]) : null;
}
