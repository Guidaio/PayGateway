using System.Threading.Channels;
using System.Text;
using System.Text.Json;

namespace PayGateway.Api.Services;

/// <summary>
/// Background service that delivers webhook notifications with retry.
/// </summary>
public class WebhookDeliveryService : BackgroundService, IWebhookDeliveryService
{
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
            await DeliverWithRetryAsync(notification, stoppingToken);
        }
    }

    private async Task DeliverWithRetryAsync(WebhookNotification notification, CancellationToken ct)
    {
        const int maxAttempts = 3;
        var delays = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var json = JsonSerializer.Serialize(notification.Payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(notification.Url, content, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Webhook delivered to {Url}, attempt {Attempt}", notification.Url, attempt + 1);
                    return;
                }

                _logger.LogWarning("Webhook to {Url} returned {StatusCode}, attempt {Attempt}", notification.Url, response.StatusCode, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook delivery to {Url} failed, attempt {Attempt}", notification.Url, attempt + 1);
            }

            if (attempt < maxAttempts - 1)
                await Task.Delay(delays[attempt], ct);
        }

        _logger.LogError("Webhook delivery to {Url} failed after {Attempts} attempts", notification.Url, maxAttempts);
    }

    private record WebhookNotification(string Url, object Payload);
}
