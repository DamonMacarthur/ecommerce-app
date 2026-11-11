using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace ECommerce.Ordering.Api.Tests;

public sealed class CreateOrderTests : IDisposable
{
    private readonly OrderingApiFactory _factory = new();
    private readonly HttpClient _client;

    public CreateOrderTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact(DisplayName = "Scenario 4 - valid order returns 201, Location, Confirmed and total 239.98")]
    public async Task ValidOrder_Returns201WithLocationAndTotal()
    {
        var response = await PostOrderAsync("""{"customer":"Ada","items":[{"productId":1,"quantity":2}]}""");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal("Confirmed", order.Status);
        Assert.Equal(239.98m, order.Total);
    }

    [Fact(DisplayName = "Scenario 5 - empty items returns 400")]
    public async Task EmptyItems_Returns400()
    {
        var response = await PostOrderAsync("""{"customer":"Ada","items":[]}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Scenario 6 - unknown product returns 400 naming product 99")]
    public async Task UnknownProduct_Returns400NamingTheProduct()
    {
        var response = await PostOrderAsync("""{"customer":"Ada","items":[{"productId":99,"quantity":1}]}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("99", await response.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Scenario 7 - blank customer returns 400")]
    public async Task BlankCustomer_Returns400()
    {
        var response = await PostOrderAsync("""{"customer":"","items":[{"productId":1,"quantity":1}]}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Scenario 8 - zero quantity returns 400")]
    public async Task ZeroQuantity_Returns400()
    {
        var response = await PostOrderAsync("""{"customer":"Ada","items":[{"productId":1,"quantity":0}]}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Scenario 9 - missing items property returns 400")]
    public async Task MissingItemsProperty_Returns400()
    {
        var response = await PostOrderAsync("""{"customer":"Ada"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostOrderAsync(string json) =>
        _client.PostAsync("/api/orders", new StringContent(json, Encoding.UTF8, "application/json"));

    // The consumer's view of the contract: Status travels as a string.
    private sealed record OrderResponse(int Id, string Customer, string Status, decimal Total);
}
