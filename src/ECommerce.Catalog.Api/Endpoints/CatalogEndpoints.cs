using ECommerce.Catalog.Api.Data;
using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products").WithTags("Catalog");

        group.MapGet("/", async (CatalogDbContext db) =>
            await db.Products.AsNoTracking().ToListAsync())
            .WithName("GetProducts");

        group.MapGet("/{id:int}", async (int id, CatalogDbContext db) =>
            await db.Products.FindAsync(id) is { } product
                ? Results.Ok(product)
                : Results.NotFound())
            .WithName("GetProductById");

        group.MapPost("/", async (CreateProductRequest request, CatalogDbContext db) =>
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                AvailableStock = request.AvailableStock
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.CreatedAtRoute("GetProductById", new { id = product.Id }, product);
        })
        .WithName("CreateProduct");

        // Set (or replace) a promotion on a product.
        group.MapPost("/{id:int}/promo", async (int id, AddPromoRequest request, CatalogDbContext db) =>
        {
            if (request.Percent is < 1 or > 100)
            {
                return Results.BadRequest("The promo percentage must be between 1 and 100.");
            }

            if (request.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return Results.BadRequest("The promo validity date must not be in the past.");
            }

            var product = await db.Products.FindAsync(id);
            if (product is null)
            {
                return Results.NotFound();
            }

            product.PromoPercent = request.Percent;
            product.PromoValidUntil = request.ValidUntil;
            await db.SaveChangesAsync();

            return Results.Ok(product);
        })
        .WithName("AddPromo");

        return group;
    }
}

public record CreateProductRequest(string Name, string? Description, decimal Price, int AvailableStock);
public record AddPromoRequest(int Percent, DateOnly ValidUntil);
