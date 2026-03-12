namespace PayGateway.Domain.Entities;

/// <summary>
/// Represents a payment (PIX or card) processed by the gateway.
/// </summary>
public class Payment
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }

    /// <summary>PIX key (email, CPF, phone, or random). Used when Method is Pix.</summary>
    public string? PixKey { get; set; }

    /// <summary>Type of PIX key. Used when Method is Pix.</summary>
    public PixKeyType? PixKeyType { get; set; }

    /// <summary>Last 4 digits of card. Used when Method is Card.</summary>
    public string? CardLast4 { get; set; }

    /// <summary>Card brand. Used when Method is Card.</summary>
    public CardBrand? CardBrand { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Payment method: PIX (instant) or Card.
/// </summary>
public enum PaymentMethod
{
    Pix,
    Card
}

/// <summary>
/// Payment lifecycle status.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded,
    Cancelled
}

/// <summary>
/// PIX key type (Brazil instant payment).
/// </summary>
public enum PixKeyType
{
    Email,
    Cpf,
    Phone,
    Random
}

/// <summary>
/// Card brand for card payments.
/// </summary>
public enum CardBrand
{
    Visa,
    Mastercard,
    Elo,
    Amex
}
