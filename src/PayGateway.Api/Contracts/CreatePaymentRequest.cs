using PayGateway.Domain.Entities;

namespace PayGateway.Api.Contracts;

/// <summary>
/// Request to create a payment (PIX or card).
/// </summary>
public record CreatePaymentRequest
{
    public string MerchantId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "BRL";
    public PaymentMethod Method { get; init; }

    /// <summary>PIX key (required when Method is Pix).</summary>
    public string? PixKey { get; init; }

    /// <summary>PIX key type (required when Method is Pix).</summary>
    public PixKeyType? PixKeyType { get; init; }

    /// <summary>Last 4 digits of card (required when Method is Card).</summary>
    public string? CardLast4 { get; init; }

    /// <summary>Card brand (required when Method is Card).</summary>
    public CardBrand? CardBrand { get; init; }
}
