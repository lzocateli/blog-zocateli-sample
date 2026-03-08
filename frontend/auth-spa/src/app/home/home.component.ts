// src/app/home/home.component.ts — Componente com login e consumo de API
import { Component, OnInit, OnDestroy, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { MsalService, MsalBroadcastService } from "@azure/msal-angular";
import { InteractionStatus } from "@azure/msal-browser";
import { Subject, filter, takeUntil } from "rxjs";

@Component({
    selector: "app-home",
    standalone: true,
    imports: [CommonModule],
    template: `
    <div *ngIf="!isAutenticado">
      <h2>Bem-vindo</h2>
      <button (click)="login()">Login com Azure Entra ID</button>
    </div>
    <div *ngIf="isAutenticado">
      <h2>Olá, {{ nomeUsuario }}!</h2>
      <button (click)="chamarApi()">Chamar API Protegida</button>
      <button (click)="logout()">Logout</button>
      <pre *ngIf="dadosApi">{{ dadosApi | json }}</pre>
    </div>
  `,
})
export class HomeComponent implements OnInit, OnDestroy {
    private msalService = inject(MsalService);
    private broadcastService = inject(MsalBroadcastService);
    private http = inject(HttpClient);
    private destroy$ = new Subject<void>();

    isAutenticado = false;
    nomeUsuario = "";
    dadosApi: any = null;

    ngOnInit(): void {
        // Observar mudanças no estado de autenticação
        this.broadcastService.inProgress$
            .pipe(
                filter((status) => status === InteractionStatus.None),
                takeUntil(this.destroy$)
            )
            .subscribe(() => {
                const accounts = this.msalService.instance.getAllAccounts();
                this.isAutenticado = accounts.length > 0;
                if (this.isAutenticado) {
                    this.nomeUsuario = accounts[0].name ?? accounts[0].username;
                }
            });
    }

    login(): void {
        // Inicia o fluxo de login via redirect (Authorization Code + PKCE)
        this.msalService.loginRedirect({
            scopes: ["openid", "profile", "email", "api://minha-api/User.Read"],
        });
    }

    logout(): void {
        this.msalService.logoutRedirect({
            postLogoutRedirectUri: "http://localhost:4200",
        });
    }

    chamarApi(): void {
        // O MsalInterceptor injeta o access token automaticamente
        this.http.get("https://api.seudominio.com/api/dados").subscribe({
            next: (data) => (this.dadosApi = data),
            error: (err) => console.error("Erro ao chamar API:", err),
        });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
