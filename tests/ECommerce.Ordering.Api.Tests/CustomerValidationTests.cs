using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace ECommerce.Ordering.Api.Tests;

public sealed class CustomerValidationTests : IDisposable
{
    private readonly OrderingApiFactory _factory = new();
    private readonly HttpClient _client;

    public CustomerValidationTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact(DisplayName = "Customer name of exactly 200 characters is accepted")]
    public async Task CustomerAtMaxLength_Returns201()
    {
        var customer = new string('x', 200);

        var response = await PostOrderAsync(customer);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(customer, order.Customer);
    }

    [Fact(DisplayName = "Customer name of 201 characters returns 400 with the length message")]
    public async Task CustomerOverMaxLength_Returns400WithMessage()
    {
        var response = await PostOrderAsync(new string('x', 201));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "The customer name must not exceed 200 characters.",
            await response.Content.ReadFromJsonAsync<string>());
    }

    [Fact(DisplayName = "Whitespace-only customer name returns 400 with the required message")]
    public async Task WhitespaceCustomer_Returns400WithMessage()
    {
        var response = await PostOrderAsync("   \t ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A customer name is required.", await response.Content.ReadFromJsonAsync<string>());
    }

    [Fact(DisplayName = "201 whitespace characters are reported as missing, not as too long")]
    public async Task LongWhitespaceCustomer_ReportsRequiredFirst()
    {
        var response = await PostOrderAsync(new string(' ', 201));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("A customer name is required.", await response.Content.ReadFromJsonAsync<string>());
    }

    private Task<HttpResponseMessage> PostOrderAsync(string customer) =>
        _client.PostAsJsonAsync(
            "/api/orders",
            new { customer, items = new[] { new { productId = 1, quantity = 1 } } });

    private sealed record OrderResponse(int Id, string Customer, string Status, decimal Total);
}
