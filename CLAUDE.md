# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Prerequisites: .NET 10 SDK; run `dotnet dev-certs https --trust` once (otherwise the Aspire dashboard fails with `UntrustedRoot`). Docker is not needed.

```bash
dotnet build ECommerce.slnx                       # build everything
dotnet run --project src/ECommerce.AppHost        # start all services + Aspire dashboard
```

Open the dashboard "Login URL" printed in the terminal (`AppHost/Properties/launchSettings.json` pins the dashboard to `https://localhost:17089`, or `http://localhost:15202` for the http profile), then click the `web` resource. Stop with Ctrl+C.

There is no test project, linter, CI, Dockerfile or `.claude/` config yet. Don't assume `dotnet test` works.

## Architecture

Microservices orchestrated by .NET Aspire (`src/ECommerce.AppHost/AppHost.cs`):

```
Web (Blazor Server) -> Gateway (YARP) -> Catalog.Api
                                      -> Ordering.Api -> Catalog.Api (sync HTTP)
```

- **Service discovery is the wiring.** Resource names in `AppHost.cs` (`catalog`, `ordering`, `gateway`, `web`) are the only addresses. Clients use `https+http://<name>` base addresses (e.g. `Ordering.Api/Program.cs`, `Web/Program.cs`, `Gateway/appsettings.json`), resolved at runtime via `ServiceDefaults/Extensions.cs`. Never hard-code URLs. A new service must call `AddServiceDefaults()` (gives discovery, resilience, OpenTelemetry, health checks) and be registered in `AppHost.cs` with `.WithReference`/`.WaitFor`.
- **Gateway routing** lives in `Gateway/appsettings.json` (`/catalog/*`, `/ordering/*`, prefix stripped by `PathRemovePrefix`). The Web app only talks to the gateway, so paths in `Web/Services/*ApiClient.cs` include the prefix (`/catalog/api/products`), whereas Ordering calls Catalog directly without prefix (`/api/products/{id}`).
- **Order creation** (`Ordering.Api/Endpoints/OrderingEndpoints.cs`): for each line it calls Catalog via `CatalogServiceClient`, copies name and price into `OrderItem` (snapshot), then sets status Pending -> Confirmed and saves. API DTOs are duplicated per project (`ProductDto`/`CatalogProduct`, `OrderDto`...); there is no shared contracts project, so change both sides together.
- **Data**: each API has its own EF Core `UseInMemoryDatabase`, created with `EnsureCreated()` at startup. Catalog seeds 4 products via `HasData` in `CatalogDbContext`. No migrations; data is lost on restart.

## Known gaps / gotchas

- Stock (`AvailableStock`) is neither checked nor decremented on order; the order endpoint rejects a blank/oversized `Customer` (max 200, mirrors `OrderingDbContext`), an empty item list and `Quantity <= 0` with a 400.
- `/health` and `/alive` are mapped only in Development (`MapDefaultEndpoints`).
- The cart UI only supports adding items (no remove/adjust), despite the README.
- `README.md` is the French training-course guide; each project also has a small README listing its files.
