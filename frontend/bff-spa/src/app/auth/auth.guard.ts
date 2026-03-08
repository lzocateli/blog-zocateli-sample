// src/app/auth/auth.guard.ts — Guard para proteger rotas (Angular 16+ functional guard)
import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "./auth.service";
import { map, take } from "rxjs";

export const authGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.verificarAutenticacao().pipe(
        take(1),
        map((usuario) => {
            if (usuario?.isAuthenticated) {
                return true;
            }
            // Redirecionar para login no BFF, passando a URL atual como returnUrl
            authService.login(window.location.pathname);
            return false;
        })
    );
};
