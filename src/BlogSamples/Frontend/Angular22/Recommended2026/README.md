# Angular 22 - Recommended Structure (2026)

Este exemplo implementa a estrutura citada no artigo, em formato mais complexo e com separacao clara por responsabilidade.

## Estrutura

```text
Recommended2026/
  src/
    main.ts
    app/
      app.config.ts
      app.routes.ts
      models/
        user.model.ts
      core/
        services/
          api/
            user-api.service.ts
          state/
            user-store.service.ts
      shared/
        components/
          loading-spinner/
            loading-spinner.component.ts
        pipes/
          user-role-label.pipe.ts
      features/
        dashboard/
          dashboard.component.ts
          dashboard.component.html
          dashboard.component.css
          dashboard.routes.ts
        users/
          users.routes.ts
          user-list/
            user-list.component.ts
            user-list.component.html
            user-list.component.css
          user-detail/
            user-detail.component.ts
            user-detail.component.html
            user-detail.component.css
```

## O que este exemplo cobre

- Standalone components e lazy loading por feature
- Store com Signals em `core/services/state`
- Camada de API separada em `core/services/api`
- Componentes compartilhados e pipe em `shared`
- Rotas por modulo de feature (`dashboard.routes.ts` e `users.routes.ts`)
- Filtros, estatisticas derivadas e atualizacao de estado sem NgRx

## Como estudar

1. Comece por `app/app.routes.ts`
2. Veja o fluxo em `features/dashboard/dashboard.component.ts`
3. Entenda o estado em `core/services/state/user-store.service.ts`
4. Veja reutilizacao no modulo `features/users/*`

## Observacao

Este pacote e um exemplo de arquitetura. Para executar em um app Angular real, copie a pasta `src/` para um projeto Angular 22 gerado por `ng new` e ajuste o bootstrap conforme seu `main.ts`.
