using System.Net;
using System.Net.Http.Json;
using PayGateway.Api.Contracts;
using PayGateway.Api.Endpoints;
using PayGateway.Domain.Entities;
using PayGateway.Tests.WebApplicationFactory;
using Xunit;

namespace PayGateway.Tests.Integration;

public class PaymentEndpointsTests : IClassFixture<PayGatewayWebApplicationFactory>
{
    private readonly PayGatewayWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentEndpointsTests(PayGatewayWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CreatePayment_Pix_ValidRequest_Returns201()
    {
        var idempotencyKey = "pay-pix-" + Guid.NewGuid().ToString("N")[..8];
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var request = new CreatePaymentRequest
        {
            MerchantId = "merchant-123",
            Amount = 99.99m,
            Currency = "BRL",
            Method = PaymentMethod.Pix,
            PixKey = "user@example.com",
            PixKeyType = PixKeyType.Email
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal("merchant-123", payment.MerchantId);
        Assert.Equal(99.99m, payment.Amount);
        Assert.Equal("Pix", payment.Method);
        Assert.Equal("Completed", payment.Status);
        Assert.NotEqual(Guid.Empty, payment.Id);
    }

    [Fact]
    public async Task CreatePayment_Idempotent_Returns200WithExisting()
    {
        var idempotencyKey = "pay-idem-" + Guid.NewGuid().ToString("N")[..8];
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var request = new CreatePaymentRequest
        {
            MerchantId = "merchant-idem",
            Amount = 50m,
            Currency = "BRL",
            Method = PaymentMethod.Pix,
            PixKey = "idem@test.com",
            PixKeyType = PixKeyType.Email
        };

        var first = await _client.PostAsJsonAsync("/api/v1/payments", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstPayment = await first.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(firstPayment);

        var second = await _client.PostAsJsonAsync("/api/v1/payments", request);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondPayment = await second.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(secondPayment);
        Assert.Equal(firstPayment.Id, secondPayment.Id);
    }

    [Fact]
    public async Task CreatePayment_WithoutIdempotencyKey_Returns400()
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");

        var request = new CreatePaymentRequest
        {
            MerchantId = "merchant",
            Amount = 10m,
            Method = PaymentMethod.Pix,
            PixKey = "a@b.com",
            PixKeyType = PixKeyType.Email
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreatePayment_WithoutApiKey_Returns401()
    {
        var client = _factory.CreateUnauthenticatedClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "pay-401");

        var request = new CreatePaymentRequest
        {
            MerchantId = "merchant",
            Amount = 10m,
            Method = PaymentMethod.Pix,
            PixKey = "a@b.com",
            PixKeyType = PixKeyType.Email
        };

        var response = await client.PostAsJsonAsync("/api/v1/payments", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_Pix_MissingPixKey_Returns422()
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", "pay-422-" + Guid.NewGuid().ToString("N")[..8]);

        var request = new CreatePaymentRequest
        {
            MerchantId = "merchant",
            Amount = 10m,
            Method = PaymentMethod.Pix,
            PixKey = null,
            PixKeyType = PixKeyType.Email
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_Card_ValidRequest_Returns201()
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", "pay-card-" + Guid.NewGuid().ToString("N")[..8]);

        var request = new CreatePaymentRequest
        {
            MerchantId = "merchant-card",
            Amount = 150m,
            Currency = "BRL",
            Method = PaymentMethod.Card,
            CardLast4 = "4242",
            CardBrand = CardBrand.Visa
        };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal("Card", payment.Method);
        Assert.Equal("Processing", payment.Status);
        Assert.Equal("4242", payment.CardLast4);
    }

    [Fact]
    public async Task GetPaymentById_Existing_Returns200()
    {
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        var key = "pay-get-" + Guid.NewGuid().ToString("N")[..8];
        _client.DefaultRequestHeaders.Add("Idempotency-Key", key);

        var createRequest = new CreatePaymentRequest
        {
            MerchantId = "merchant",
            Amount = 25m,
            Method = PaymentMethod.Pix,
            PixKey = "get@test.com",
            PixKeyType = PixKeyType.Email
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/payments", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/v1/payments/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal(created.Id, payment.Id);
    }

    [Fact]
    public async Task GetPaymentById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_Returns200_WithoutAuth()
    {
        var client = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
