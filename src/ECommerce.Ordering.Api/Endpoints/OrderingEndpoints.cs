using ECommerce.Ordering.Api.Data;
using ECommerce.Ordering.Api.Models;
using ECommerce.Ordering.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Api.Endpoints;

public static class OrderingEndpoints
{
    // Keep in sync with HasMaxLength on Order.Customer in OrderingDbContext.
    private const int MaxCustomerLength = 200;

    public static RouteGroupBuilder MapOrderingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/orders").WithTags("Ordering");

        group.MapGet("/", async (OrderingDbContext db) =>
            await db.Orders.AsNoTracking().Include(o => o.Items).ToListAsync())
            .WithName("GetOrders");

        group.MapGet("/{id:int}", async (int id, OrderingDbContext db) =>
            await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id) is { } order
                ? Results.Ok(order)
                : Results.NotFound())
            .WithName("GetOrderById");

        group.MapPost("/", async (
            CreateOrderRequest request,
            OrderingDbContext db,
            CatalogServiceClient catalog,
            CancellationToken ct) =>
        {
            if (ValidateOrderRequest(request) is { } error)
            {
                return Results.BadRequest(error);
            }

            var order = new Order
            {
                Customer = request.Customer,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OrderStatus.Pending
            };

            // Validate each product against the Catalog service (sync HTTP call).
            foreach (var item in request.Items)
            {
                var product = await catalog.GetProductAsync(item.ProductId, ct);
                if (product is null)
                {
                    return Results.BadRequest($"Product {item.ProductId} does not exist.");
                }

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.CurrentPrice,
                    Quantity = item.Quantity
                });
            }

            order.Status = OrderStatus.Confirmed;
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);

            return Results.CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
        })
        .WithName("CreateOrder");

        return group;
    }

    private static string? ValidateOrderRequest(CreateOrderRequest request)
    {
        string? error = null;
        if (request.Customer != null && request.Customer.Trim().Length > 0)
        {
            if (request.Customer.Length <= MaxCustomerLength)
            {
                if (request.Items != null)
                {
                    if (request.Items.Count > 0)
                    {
                        for (var i = 0; i < request.Items.Count; i++)
                        {
                            if (request.Items[i].Quantity <= 0)
                            {
                                if (error == null)
                                {
                                    error = $"The quantity for product {request.Items[i].ProductId} must be greater than zero.";
                                }
                            }
                        }
                    }
                    else
                    {
                        error = "An order must contain at least one item.";
                    }
                }
                else
                {
                    error = "An order must contain at least one item.";
                }
            }
            else
            {
                error = $"The customer name must not exceed {MaxCustomerLength} characters.";
            }
        }
        else
        {
            error = "A customer name is required.";
        }

        return error;
    }
}

public record CreateOrderRequest(string Customer, List<OrderLine> Items);
public record OrderLine(int ProductId, int Quantity);
