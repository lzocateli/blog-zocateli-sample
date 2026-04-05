// src/app/app.routes.ts — Rotas com MsalGuard para proteger acesso
import { Routes } from "@angular/router";
import { MsalGuard } from "@azure/msal-angular";
import { HomeComponent } from "./home/home.component";
import { DashboardComponent } from "./dashboard/dashboard.component";

export const routes: Routes = [
    { path: "", component: HomeComponent },
    {
        path: "dashboard",
        component: DashboardComponent,
        canActivate: [MsalGuard], // Rota protegida — requer autenticação
    },
];
