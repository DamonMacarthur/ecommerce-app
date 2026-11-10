namespace ECommerce.Catalog.Api.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int AvailableStock { get; set; }

    /// <summary>Promotional discount, 1-100 (%). Null when the product has no promotion.</summary>
    public int? PromoPercent { get; set; }

    /// <summary>Last day (inclusive, UTC) on which the promotion applies.</summary>
    public DateOnly? PromoValidUntil { get; set; }

    /// <summary>Price to charge today: the discounted price while the promotion is valid, otherwise <see cref="Price"/>.</summary>
    public decimal CurrentPrice =>
        PromoPercent is { } percent
        && PromoValidUntil is { } until
        && DateOnly.FromDateTime(DateTime.UtcNow) <= until
            ? Math.Round(Price * (100 - percent) / 100m, 2)
            : Price;
}
