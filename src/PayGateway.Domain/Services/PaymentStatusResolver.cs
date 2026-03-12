using PayGateway.Domain.Entities;

namespace PayGateway.Domain.Services;

/// <summary>
/// Resolves the initial payment status based on payment method.
/// PIX is instant (Completed); Card is simulated (Processing).
/// </summary>
public static class PaymentStatusResolver
{
    public static PaymentStatus GetInitialStatus(PaymentMethod method)
    {
        return method == PaymentMethod.Pix ? PaymentStatus.Completed : PaymentStatus.Processing;
    }
}
