// src/app/app.config.ts — Configuração standalone do Angular 16+ com BFF
import { ApplicationConfig } from "@angular/core";
import { provideRouter } from "@angular/router";
import {
    provideHttpClient,
    withInterceptors,
    withXsrfConfiguration,
} from "@angular/common/http";
import { routes } from "./app.routes";
import { csrfInterceptor } from "./auth/csrf.interceptor";

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(routes),
        provideHttpClient(
            // Configuração XSRF nativa do Angular (redundância de segurança)
            withXsrfConfiguration({
                cookieName: "XSRF-TOKEN",
                headerName: "X-XSRF-TOKEN",
            }),
            // Interceptor CSRF customizado para controle explícito
            withInterceptors([csrfInterceptor])
        ),
    ],
};
