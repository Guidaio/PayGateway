using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayGateway.Infrastructure.Persistence;

namespace PayGateway.Tests.WebApplicationFactory;

/// <summary>
/// Custom WebApplicationFactory that uses InMemory database for isolated integration tests.
/// Injects ApiKey:Value = "test-key" for authenticated requests.
/// </summary>
public class PayGatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string ApiKeyHeader = "X-API-KEY";
    private const string ApiKeyValue = "test-key";

    private static readonly string DatabaseName = "PayGateway_Test_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ApiKey:Value", ApiKeyValue);

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PaymentDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<PaymentDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));
        });
    }

    /// <summary>
    /// Creates an HttpClient with X-API-KEY header for authenticated requests.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = base.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyHeader, ApiKeyValue);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient without the API key header (for 401 tests).
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        return base.CreateClient();
    }
}
