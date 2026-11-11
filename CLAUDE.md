# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Prerequisites: .NET 10 SDK; run `dotnet dev-certs https --trust` once (otherwise the Aspire dashboard fails with `UntrustedRoot`). Docker must be running (Aspire starts a PostgreSQL container on host port 5433 with a persistent volume). Set the database password once: `dotnet user-secrets set Parameters:postgres-password <value> --project src/ECommerce.AppHost`.

```bash
dotnet build ECommerce.slnx                       # build everything
dotnet run --project src/ECommerce.AppHost        # start all services + Aspire dashboard
```

Open the dashboard "Login URL" printed in the terminal (`AppHost/Properties/launchSettings.json` pins the dashboard to `https://localhost:17089`, or `http://localhost:15202` for the http profile), then click the `web` resource. Stop with Ctrl+C.

There is no test project, linter, CI or Dockerfile. `dotnet test` succeeds but runs zero tests, so a green "Tests" step proves nothing.

### Project workflow (`.claude/`)

- `/smoke-test` runs `.claude/skills/smoke-test/smoke.sh`: GETs the catalog and POSTs a valid and an unknown-product order through the gateway at `http://localhost:5067` (pinned in `Gateway/Properties/launchSettings.json`). If something already answers there it reuses it and leaves it running; each run adds an order to that instance's in-memory DB. It only checks HTTP status codes, not amounts.
- `/revue-diff [focus]` is a read-only review of the branch + uncommitted diff.
- `/ship <title>` runs `.claude/skills/ship/verifier.sh` (build, test, `dotnet format --verify-no-changes` on changed `.cs` files), then commit, push, and `gh pr create` using `modele-pr.md`. Push and PR creation ask for confirmation; never force-push or merge.
- `settings.json` denies reading `.env` (it is gitignored) and secrets/key files.
- Before trusting a `curl` against `:5067`, check which copy is serving it: another clone of this repo (e.g. under `Downloads`) can be the one running, and then your changes are not what answers.

## Architecture

Microservices orchestrated by .NET Aspire (`src/ECommerce.AppHost/AppHost.cs`):

```
Web (Blazor Server) -> Gateway (YARP) -> Catalog.Api
                                      -> Ordering.Api -> Catalog.Api (sync HTTP)
```

- **Service discovery is the wiring.** Resource names in `AppHost.cs` (`catalog`, `ordering`, `gateway`, `web`) are the only addresses. Clients use `https+http://<name>` base addresses (e.g. `Ordering.Api/Program.cs`, `Web/Program.cs`, `Gateway/appsettings.json`), resolved at runtime via `ServiceDefaults/Extensions.cs`. Never hard-code URLs. A new service must call `AddServiceDefaults()` (gives discovery, resilience, OpenTelemetry, health checks) and be registered in `AppHost.cs` with `.WithReference`/`.WaitFor`.
- **Gateway routing** lives in `Gateway/appsettings.json` (`/catalog/*`, `/ordering/*`, prefix stripped by `PathRemovePrefix`). The Web app only talks to the gateway, so paths in `Web/Services/*ApiClient.cs` include the prefix (`/catalog/api/products`), whereas Ordering calls Catalog directly without prefix (`/api/products/{id}`).
- **Order creation** (`Ordering.Api/Endpoints/OrderingEndpoints.cs`): for each line it calls Catalog via `CatalogServiceClient`, copies name and price into `OrderItem` (snapshot), then sets status Pending -> Confirmed and saves. API DTOs are duplicated per project (`ProductDto`/`CatalogProduct`, `OrderDto`...); there is no shared contracts project, so change both sides together.
- **Data**: one PostgreSQL server (`postgres` in `AppHost.cs`, volume + port 5433) with a database per API (`catalogdb`, `orderingdb`), registered via `AddNpgsqlDbContext` and migrated with `Migrate()` at startup (`Data/Migrations` in each API). Catalog seeds 4 products via `HasData` (inside `InitialCreate`; Npgsql resets the id sequence after the seed). Data persists across restarts; to reset it, delete the `postgres` Docker volume. Add a migration with `dotnet ef migrations add <Name> --output-dir Data/Migrations --project src/ECommerce.<Api>.Api --startup-project src/ECommerce.<Api>.Api` (a design-time factory avoids needing a live database). Integration tests still use the EF InMemory provider (swapped in by the test factories; `Program.cs` falls back to `EnsureCreated()` for non-relational providers).
- **Promotions**: `Product` has `PromoPercent` + `PromoValidUntil` (inclusive, UTC day); `Product.CurrentPrice` is computed at request time and is what Ordering bills (`UnitPrice`) and Web displays. `POST /api/products/{id}/promo` sets one; Mechanical Keyboard is seeded at 30% until 2026-11-15.

## Known gaps / gotchas

- Stock (`AvailableStock`) is neither checked nor decremented on order.
- `POST /api/orders` rejects with a 400 a blank/oversized `Customer` (max 200, mirrors `OrderingDbContext`), an empty item list and `Quantity <= 0`.
- Pricing risks: `CatalogProduct.CurrentPrice` (Ordering) is a non-nullable `decimal`, so a Catalog that does not send it makes Ordering bill 0; `POST /api/products/{id}/promo` is unauthenticated, reachable through the gateway, and accepts 100%; the promo seeded on the keyboard (2026-11-15) is demo data and the date is hard-coded.
- No API authentication anywhere.
- `/health` and `/alive` are mapped only in Development (`MapDefaultEndpoints`).
- The cart UI only supports adding items (no remove/adjust), despite the README.
- `README.md` is the French training-course guide; each project also has a small README listing its files.
