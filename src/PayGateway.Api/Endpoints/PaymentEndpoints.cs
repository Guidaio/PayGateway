using Microsoft.EntityFrameworkCore;
using PayGateway.Api.Contracts;
using PayGateway.Api.ProblemDetails;
using PayGateway.Api.Services;
using PayGateway.Domain.Entities;
using PayGateway.Infrastructure.Persistence;

namespace PayGateway.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments")
            .WithTags("Payments")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetPaymentById)
            .WithName("GetPaymentById")
            .WithOpenApi()
            .Produces<PaymentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreatePayment)
            .WithName("CreatePayment")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create payment (idempotent)";
                operation.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
                {
                    Name = "Idempotency-Key",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Required = false,
                    Description = "Unique key for idempotency. Same key returns existing payment (200) instead of creating duplicate."
                });
                return operation;
            })
            .Produces<PaymentResponse>(StatusCodes.Status201Created)
            .Produces<PaymentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> GetPaymentById(
        Guid id,
        PaymentDbContext db,
        CancellationToken ct)
    {
        var payment = await db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (payment is null)
            return ProblemDetailsExtensions.NotFound($"Payment {id} not found");

        return Results.Ok(ToResponse(payment));
    }

    private static async Task<IResult> CreatePayment(
        CreatePaymentRequest request,
        PaymentDbContext db,
        IWebhookDeliveryService webhookService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return ProblemDetailsExtensions.BadRequest("Idempotency-Key header is required");

        var existing = await db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);

        if (existing is not null)
            return Results.Ok(ToResponse(existing));

        if (string.IsNullOrWhiteSpace(request.MerchantId))
            return ProblemDetailsExtensions.BadRequest("MerchantId is required");

        if (request.Amount <= 0)
            return ProblemDetailsExtensions.BadRequest("Amount must be positive");

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency.Trim();

        if (request.Method == PaymentMethod.Pix)
        {
            if (string.IsNullOrWhiteSpace(request.PixKey))
                return ProblemDetailsExtensions.UnprocessableEntity("PixKey is required when Method is Pix");
            if (request.PixKeyType is null)
                return ProblemDetailsExtensions.UnprocessableEntity("PixKeyType is required when Method is Pix");
        }
        else if (request.Method == PaymentMethod.Card)
        {
            if (string.IsNullOrWhiteSpace(request.CardLast4) || request.CardLast4.Length != 4)
                return ProblemDetailsExtensions.UnprocessableEntity("CardLast4 must be 4 digits when Method is Card");
            if (request.CardBrand is null)
                return ProblemDetailsExtensions.UnprocessableEntity("CardBrand is required when Method is Card");
        }

        var now = DateTime.UtcNow;
        var status = request.Method == PaymentMethod.Pix
            ? PaymentStatus.Completed
            : PaymentStatus.Processing;

        var payment = new Domain.Entities.Payment
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            MerchantId = request.MerchantId.Trim(),
            Amount = request.Amount,
            Currency = currency,
            Method = request.Method,
            Status = status,
            PixKey = request.Method == PaymentMethod.Pix ? request.PixKey?.Trim() : null,
            PixKeyType = request.Method == PaymentMethod.Pix ? request.PixKeyType : null,
            CardLast4 = request.Method == PaymentMethod.Card ? request.CardLast4 : null,
            CardBrand = request.Method == PaymentMethod.Card ? request.CardBrand : null,
            CreatedAtUtc = now
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var response = ToResponse(payment);

        if (!string.IsNullOrWhiteSpace(request.WebhookUrl) && Uri.TryCreate(request.WebhookUrl.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var webhookPayload = new
            {
                eventType = "payment.created",
                paymentId = payment.Id,
                merchantId = payment.MerchantId,
                amount = payment.Amount,
                currency = payment.Currency,
                method = payment.Method.ToString(),
                status = payment.Status.ToString(),
                createdAtUtc = payment.CreatedAtUtc
            };
            await webhookService.EnqueueAsync(request.WebhookUrl.Trim(), webhookPayload, ct);
        }

        return Results.Created($"/api/v1/payments/{payment.Id}", response);
    }

    private static PaymentResponse ToResponse(Domain.Entities.Payment p)
    {
        return new PaymentResponse(
            p.Id,
            p.IdempotencyKey,
            p.MerchantId,
            p.Amount,
            p.Currency,
            p.Method.ToString(),
            p.Status.ToString(),
            p.PixKey,
            p.PixKeyType?.ToString(),
            p.CardLast4,
            p.CardBrand?.ToString(),
            p.CreatedAtUtc);
    }
}

public record PaymentResponse(
    Guid Id,
    string IdempotencyKey,
    string MerchantId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    string? PixKey,
    string? PixKeyType,
    string? CardLast4,
    string? CardBrand,
    DateTime CreatedAtUtc);
