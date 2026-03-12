namespace PayGateway.Api.Services;

/// <summary>
/// Enqueues webhook notifications for delivery.
/// </summary>
public interface IWebhookDeliveryService
{
    ValueTask EnqueueAsync(string url, object payload, CancellationToken ct = default);
}
