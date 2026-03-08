// src/app/auth/auth.service.ts — Serviço de autenticação via BFF (sem MSAL!)
import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { BehaviorSubject, Observable, catchError, of, tap } from "rxjs";

// Interface que representa o usuário autenticado
export interface UsuarioBff {
    isAuthenticated: boolean;
    nome: string;
    email: string;
    claims: { type: string; value: string }[];
}

@Injectable({ providedIn: "root" })
export class AuthService {
    private http = inject(HttpClient);

    // Estado reativo do usuário
    private usuarioSubject = new BehaviorSubject<UsuarioBff | null>(null);
    public usuario$ = this.usuarioSubject.asObservable();

    /**
     * Verifica se o usuário está autenticado consultando o BFF.
     * O cookie HttpOnly é enviado automaticamente pelo navegador.
     */
    verificarAutenticacao(): Observable<UsuarioBff | null> {
        return this.http
            .get<UsuarioBff>("/bff/user", { withCredentials: true })
            .pipe(
                tap((usuario) => this.usuarioSubject.next(usuario)),
                catchError(() => {
                    // 401 = não autenticado — comportamento esperado
                    this.usuarioSubject.next(null);
                    return of(null);
                })
            );
    }

    /**
     * Redireciona o navegador para o endpoint de login do BFF.
     * O BFF iniciará o fluxo OAuth2 Authorization Code com Azure Entra ID.
     */
    login(returnUrl: string = "/"): void {
        window.location.href = `/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`;
    }

    /**
     * Redireciona para o endpoint de logout do BFF.
     * O BFF limpa a sessão local e redireciona ao Azure Entra para logout global.
     */
    logout(): void {
        window.location.href = "/bff/logout";
    }

    /** Retorna true se o usuário está autenticado. */
    get isAutenticado(): boolean {
        return this.usuarioSubject.value?.isAuthenticated === true;
    }

    /** Retorna o nome do usuário autenticado. */
    get nomeUsuario(): string {
        return this.usuarioSubject.value?.nome ?? "";
    }
}
