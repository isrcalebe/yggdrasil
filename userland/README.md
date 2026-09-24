# userland

The Yggdrasil website: where players sign in, register and manage their account. Built with Vite, React and TypeScript, using [Bun](https://bun.sh) as package manager and runtime.

## Local development

The site talks to the API (`webapis/publicapis`), which needs PostgreSQL.

1. Start PostgreSQL, from the repository root:

   ```bash
   docker compose up -d
   ```

2. Start the API (http://localhost:8100):

   ```bash
   dotnet run --project webapis/publicapis
   ```

3. Start the site (http://localhost:5173):

   ```bash
   bun install
   bun run dev
   ```

Open http://localhost:5173.

### How the site reaches the API

In development, the Vite dev server proxies `/api`, `/connect` and `/.well-known` to the API on port 8100, so the browser only ever talks to `localhost:5173`. Keeping the site and the API on the same origin makes the sign-in cookie first-party. In production, the ASP.NET host serves the built site itself, so both share the same origin without a proxy.

The dev server always uses port 5173 (`strictPort`): OAuth redirects depend on that address.

## Scripts

| Script | Description |
|---|---|
| `bun run dev` | Dev server with hot reload |
| `bun run build` | Type-check and build into `dist/` |
| `bun run typecheck` | Type-check only |
| `bun run lint` | ESLint (type-aware, stylistic and import-sorting rules) |
| `bun run lint:fix` | ESLint with automatic fixes |
| `bun run preview` | Serve the production build locally |

## Conventions

- Import from `src/` through the `@yggdrasil/*` alias, e.g. `import { x } from "@yggdrasil/lib/x"`. Parent-relative imports (`../`) are rejected by the linter; `./` is fine for files in the same folder.
- Code style (double quotes, semicolons, two-space indentation, import order) is enforced by ESLint: run `bun run lint:fix` before committing.
