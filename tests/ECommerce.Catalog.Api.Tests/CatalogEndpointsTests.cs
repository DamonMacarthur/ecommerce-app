using System.Net;
using System.Net.Http.Json;

namespace ECommerce.Catalog.Api.Tests;

public sealed class CatalogEndpointsTests : IDisposable
{
    private readonly CatalogApiFactory _factory = new();
    private readonly HttpClient _client;

    public CatalogEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact(DisplayName = "Scenario 1 - GET /api/products returns the 4 seeded products")]
    public async Task GetProducts_ReturnsFourProducts()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        Assert.Equal(4, products.Count);
    }

    [Fact(DisplayName = "Scenario 2 - GET /api/products/1 returns the Mechanical Keyboard at 119.99")]
    public async Task GetProductById_ExistingProduct_ReturnsKeyboard()
    {
        var response = await _client.GetAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(119.99m, product.Price);
    }

    [Fact(DisplayName = "Scenario 3 - GET /api/products/999 returns 404")]
    public async Task GetProductById_UnknownProduct_Returns404()
    {
        var response = await _client.GetAsync("/api/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // The consumer's view of the contract. Price is the list price: the promotion
    // only changes CurrentPrice, so this assertion does not depend on today's date.
    private sealed record ProductResponse(int Id, string Name, decimal Price);
}
