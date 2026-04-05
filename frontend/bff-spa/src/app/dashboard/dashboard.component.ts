// src/app/dashboard/dashboard.component.ts — Consumo de API protegida via BFF proxy
import { Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { AuthService } from "../auth/auth.service";

@Component({
    selector: "app-dashboard",
    standalone: true,
    imports: [CommonModule],
    template: `
    <h2>Dashboard</h2>
    <p>Olá, {{ auth.nomeUsuario }}!</p>

    <button (click)="carregarDados()">Carregar Dados da API</button>
    <button (click)="auth.logout()">Logout</button>

    <div *ngIf="dados">
      <h3>Resposta da API:</h3>
      <pre>{{ dados | json }}</pre>
    </div>

    <div *ngIf="erro" class="erro">
      <p>Erro: {{ erro }}</p>
    </div>
  `,
})
export class DashboardComponent implements OnInit {
    auth = inject(AuthService);
    private http = inject(HttpClient);

    dados: any = null;
    erro: string | null = null;

    ngOnInit(): void {
        this.carregarDados();
    }

    carregarDados(): void {
        // A requisição vai para /api/dados no BFF (mesmo domínio)
        // O BFF faz proxy para a API real, injetando o access token
        // O cookie de sessão é enviado automaticamente pelo navegador
        this.http
            .get("/api/dados", { withCredentials: true })
            .subscribe({
                next: (data) => {
                    this.dados = data;
                    this.erro = null;
                },
                error: (err) => {
                    this.erro = `Falha ao carregar dados: ${err.status} ${err.statusText}`;
                    if (err.status === 401) {
                        this.auth.login(window.location.pathname); // Sessão expirou
                    }
                },
            });
    }
}
