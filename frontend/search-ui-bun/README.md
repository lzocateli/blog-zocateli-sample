# Search UI — Bun

Versão do projeto **Search UI** rodando com [Bun](https://bun.sh/) como runtime e package manager.

## Pré-requisitos

- [Bun](https://bun.sh/) instalado (`curl -fsSL https://bun.sh/install | bash`)

## Como executar

```bash
# Instalar dependências (~25x mais rápido que npm install)
bun install

# Iniciar servidor de desenvolvimento
bun run start
# ou diretamente:
bunx ng serve
```

## Diferenças em relação ao search-ui original (Node.js)

| Aspecto | Node.js (`search-ui/`) | Bun (`search-ui-bun/`) |
| --- | --- | --- |
| Package manager | npm | bun |
| Lockfile | `package-lock.json` | `bun.lockb` (binário) |
| Velocidade install | Base | ~25x mais rápido |
| node_modules | Sim (cópias) | Sim (hardlinks) |
| Runtime | Node.js | Bun (JavaScriptCore) |

## Estrutura

```text
search-ui-bun/
├── package.json          # Scripts com bunx ng serve
├── bunfig.toml           # Configuração do Bun
├── angular.json          # Workspace Angular
├── tsconfig.json         # TypeScript config
├── tsconfig.app.json     # TypeScript config (app)
└── src/
    ├── main.ts           # Bootstrap standalone
    ├── index.html        # HTML base
    ├── styles.css        # Estilos globais
    └── app/
        ├── app.component.ts
        ├── app.config.ts
        ├── app.routes.ts
        ├── models.ts
        ├── cliente.service.ts
        ├── clientes-grid.component.ts
        └── clientes-grid.component.html
```

## Observação

O Bun utiliza `node_modules` internamente, mas com **hardlinks** em vez de cópias, o que reduz significativamente o espaço em disco e o tempo de instalação. O lockfile binário (`bun.lockb`) também é mais rápido de gerar e ler em comparação com `package-lock.json`.
