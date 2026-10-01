# yggdrasil

Accounts and player profiles for games. Players sign up once on the Yggdrasil website and sign in to any registered game through OAuth 2.0 / OpenID Connect. Each game keeps its own data for each player, and only that game's server can write it.

## How it works

- **Identity** is the authorization server (OpenIddict on ASP.NET Core Identity). It handles accounts, cookie sessions for the website, and the OAuth endpoints (`/connect/authorize`, `/connect/token`, `/.well-known/openid-configuration`).
- **Games** registers games. Registering one creates two OAuth clients:
  - a public **game client**, which signs players in with the authorization code flow + PKCE and reads their profile (`profiles.read`);
  - a confidential **server client**, which uses client credentials to write player data (`profiles.write`). Its secret is shown only once, in the registration response.
- **Profiles** stores one profile per player per game, with free-form JSON data versioned through ETags (`If-Match`). A game server can only touch the profiles of its own game.

Access tokens are signed JWTs, so game servers can validate them locally against the published keys (JWKS).

## Repository layout

| Path | What's there |
|---|---|
| [`webapis/`](webapis) | The API: a .NET 10 modular monolith |
| [`webapis/publicapis/`](webapis/publicapis) | The ASP.NET Core host |
| [`webapis/modules/`](webapis/modules) | One folder per module (`identity`, `games`, `profiles`), each split into `contracts` and `module` |
| [`webapis/buildingblocks/`](webapis/buildingblocks) | Code shared by the modules (results, persistence, web plumbing) |
| [`webapis/tests/`](webapis/tests) | Architecture, host and integration tests |
| [`userland/`](userland) | The website (Vite, React, TypeScript), where players sign in and register. See its [README](userland/README.md) |
| [`compose.yaml`](compose.yaml) | PostgreSQL for local development |

Inside a module, each use case lives in its own folder under `Features/v{version}/` (endpoint, handler, validator), and handlers are dispatched with [Mediator](https://github.com/martinothamar/Mediator). Endpoints are versioned under `/api/v{version}/{module}`. Modules only reference each other's `contracts` projects; the architecture tests enforce this.

## Getting started

Requirements: [.NET SDK 10](https://dotnet.microsoft.com/download), [Bun](https://bun.sh) and Docker.

1. Start PostgreSQL (port 5440):

   ```bash
   docker compose up -d
   ```

2. Start the API on http://localhost:8100. In development, migrations run on startup.

   ```bash
   dotnet run --project webapis/publicapis
   ```

3. Start the website on http://localhost:5173:

   ```bash
   cd userland && bun install && bun run dev
   ```

The API reference (Scalar) is at http://localhost:8100/scalar, and the OpenAPI document at `/openapi/v1.json`. Both are disabled in production. Health checks are at `/health/live` and `/health/ready`.

### Administrators

Registering games and granting administrator rights require an administrator. To bootstrap the first one, register an account and add its id to the `Identity:Administrators` setting; it gets the role on the next startup:

```bash
dotnet user-secrets set "Identity:Administrators:0" "<account id>" --project webapis/publicapis
```

## Tests

The integration tests start PostgreSQL with Testcontainers.

```bash
cd webapis
dotnet test --project tests/architecture
dotnet test --project tests/publicapis
dotnet test --project tests/integration
```

CI ([`.github/workflows`](.github/workflows)) also checks formatting with `dotnet format whitespace` and runs lint, typecheck and build for the website.

## License

[MIT](LICENSE)
