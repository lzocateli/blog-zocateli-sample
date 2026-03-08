// src/app/app.config.ts — Configuração standalone do Angular 16+
import { ApplicationConfig, importProvidersFrom } from "@angular/core";
import { provideRouter } from "@angular/router";
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from "@angular/common/http";
import {
    MsalModule,
    MsalService,
    MsalGuard,
    MsalInterceptor,
    MsalBroadcastService,
    MSAL_INSTANCE,
    MSAL_GUARD_CONFIG,
    MSAL_INTERCEPTOR_CONFIG,
} from "@azure/msal-angular";
import {
    MSALInstanceFactory,
    MSALGuardConfigFactory,
    MSALInterceptorConfigFactory,
} from "./auth-config";
import { routes } from "./app.routes";

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(routes),
        provideHttpClient(withInterceptorsFromDi()),
        importProvidersFrom(MsalModule),
        {
            provide: HTTP_INTERCEPTORS,
            useClass: MsalInterceptor, // Interceptor injeta token nas requisições automaticamente
            multi: true,
        },
        {
            provide: MSAL_INSTANCE,
            useFactory: MSALInstanceFactory,
        },
        {
            provide: MSAL_GUARD_CONFIG,
            useFactory: MSALGuardConfigFactory,
        },
        {
            provide: MSAL_INTERCEPTOR_CONFIG,
            useFactory: MSALInterceptorConfigFactory,
        },
        MsalService,
        MsalGuard,             // Guard para proteger rotas
        MsalBroadcastService,  // Service para observar eventos de autenticação
    ],
};
