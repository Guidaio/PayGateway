using System.Threading.Channels;
using System.Text;
using System.Text.Json;

namespace PayGateway.Api.Services;

/// <summary>
/// Background service that delivers webhook notifications. Uses Polly (retry + circuit breaker) via HttpClient.
/// </summary>
public class WebhookDeliveryService : BackgroundService, IWebhookDeliveryService
{
    private const string HttpClientName = "Webhook";

    private readonly Channel<WebhookNotification> _channel = Channel.CreateUnbounded<WebhookNotification>();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(IHttpClientFactory httpClientFactory, ILogger<WebhookDeliveryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public ValueTask EnqueueAsync(string url, object payload, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(new WebhookNotification(url, payload), ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await DeliverAsync(notification, stoppingToken);
        }
    }

    private async Task DeliverAsync(WebhookNotification notification, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var json = JsonSerializer.Serialize(notification.Payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(notification.Url, content, ct);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Webhook delivered to {Url}", notification.Url);
            else
                _logger.LogWarning("Webhook to {Url} returned {StatusCode}", notification.Url, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook delivery to {Url} failed", notification.Url);
        }
    }

    private record WebhookNotification(string Url, object Payload);
}
