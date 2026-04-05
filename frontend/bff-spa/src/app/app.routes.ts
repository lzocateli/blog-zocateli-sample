// src/app/app.routes.ts — Rotas com guard de autenticação via BFF
import { Routes } from "@angular/router";
import { authGuard } from "./auth/auth.guard";

export const routes: Routes = [
    {
        path: "",
        loadComponent: () =>
            import("./home/home.component").then((m) => m.HomeComponent),
    },
    {
        path: "dashboard",
        loadComponent: () =>
            import("./dashboard/dashboard.component").then((m) => m.DashboardComponent),
        canActivate: [authGuard], // Protegida — requer autenticação via BFF
    },
    {
        path: "admin",
        loadComponent: () =>
            import("./admin/admin.component").then((m) => m.AdminComponent),
        canActivate: [authGuard],
    },
];
