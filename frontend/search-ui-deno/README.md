# Search UI — Deno

Versão do projeto **Search UI** rodando com [Deno](https://deno.land/) — **sem `node_modules`**.

## Pré-requisitos

- [Deno](https://deno.land/) 2.x instalado (`curl -fsSL https://deno.land/install.sh | sh`)

## Como executar

```bash
# Resolver dependências para o cache global do Deno (sem node_modules)
deno install

# Iniciar servidor de desenvolvimento
deno task dev
```

## Diferenças em relação ao search-ui original (Node.js)

| Aspecto | Node.js (`search-ui/`) | Deno (`search-ui-deno/`) |
| --- | --- | --- |
| Package manager | npm | deno (cache global) |
| Lockfile | `package-lock.json` | `deno.lock` |
| node_modules | Sim (~350MB) | **Não** (cache global compartilhado) |
| Import maps | Não | Sim (`deno.json`) |
| Segurança | Permissivo | Sandbox (flags `--allow-*`) |
| Runtime | Node.js (V8) | Deno (V8) |

## Estrutura

```text
search-ui-deno/
├── deno.json             # Import maps + tasks + nodeModulesDir: "none"
├── angular.json          # Workspace Angular
├── tsconfig.json         # TypeScript config
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

## Ponto-chave

Com `"nodeModulesDir": "none"` no `deno.json`, **nenhuma pasta `node_modules` é criada**. As dependências ficam no cache global do Deno (`~/.cache/deno/`), compartilhado entre todos os projetos. O resultado: esta pasta ocupa apenas o espaço do código-fonte real (~20KB), não centenas de MB.
