# ECommerce.Web

**Type:** Blazor Web App (interactive server render mode)
**Depends on:** ECommerce.ServiceDefaults, **calls the Gateway**

The web frontend. It is just another client of the system — it goes through the Gateway
exactly like an external caller, never directly to Catalog or Ordering.

## Files

| File | Responsibility |
|---|---|
| `Services/CatalogApiClient.cs` | Typed client → `/catalog/...` via the gateway. `ProductDto`. |
| `Services/OrderingApiClient.cs` | Typed client → `/ordering/...` via the gateway. Order DTOs. |
| `Components/Pages/Home.razor` | Landing page with links to Catalog and Orders. |
| `Components/Pages/Products.razor` | Catalog list + in-memory cart + place-order form. |
| `Components/Pages/Orders.razor` | Order history with a refresh button. |
| `Components/Layout/NavMenu.razor` | Navigation: Catalog / Orders. Sober sidebar surface, active link with an accent bar. |
| `wwwroot/app.css` | Design tokens (`--color-*`, `--radius-*`, `--shadow-*`, `--space-*`, typography) on `:root`, dark theme, focus ring, Bootstrap re-theming. |
| `Components/**/*.razor.css` | Scoped layout per component (CSS Grid card layouts, order status badges). |
| `Program.cs` | Registers the two typed clients pointing at `https+http://gateway`. |

## Pages / flow

1. **Catalog** (`/products`) — lists products from Catalog, lets you add items to a cart,
   enter a customer name, and place an order (POST to Ordering).
2. **Orders** (`/orders`) — lists orders from Ordering.

## Patterns

- **Typed `HttpClient`s** registered with `AddHttpClient<T>` — they inherit service discovery
  and resilience from ServiceDefaults.
- **UI as a gateway client** — both clients target `https+http://gateway`, so the UI is
  decoupled from the internal service topology, same as any other consumer.

## Styling

- **Design tokens** are defined once on `:root` in `wwwroot/app.css`; every other stylesheet
  (including the scoped `.razor.css` files) only reads them. No colour is hard-coded elsewhere.
- **Dark theme**: `prefers-color-scheme: dark` redefines the tokens; Bootstrap variables
  (`--bs-body-bg`, `--bs-body-color`, `--bs-card-*`, `--bs-btn-*`...) are bridged to the tokens,
  so cards, tables, fields, alerts and buttons follow. Text/background contrast is at least 4.5:1.
- **Typography**: system font stack, fluid headings with `clamp()`, line height 1.5.
- **Layout**: content centred with a maximum reading width; Home cards, the product list and
  the order list use `grid-template-columns: repeat(auto-fill, minmax(min(100%, ...), 1fr))`.
  The product table keeps explicit ARIA table roles while its rows are shown as cards; column
  headers are visually hidden and repeated as small labels on each card.
- **Interaction**: transitions of 150 ms, disabled under `prefers-reduced-motion: reduce`;
  `:focus-visible` shows a 3 px outline on links, buttons and fields in both themes.
- Bootstrap (`wwwroot/lib`) is still loaded unchanged; no CDN, font or JavaScript was added.

## Notes

The default Blazor template's `Counter` and `Weather` demo pages were removed.
