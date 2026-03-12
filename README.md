# PayGateway

Payment gateway simulation (PIX + card). Portfolio project aligned with Senior .NET Backend (fintech/POS) roles.

## Context

PayGateway is an API that simulates a **payment gateway** for processing PIX and card transactions. It includes webhooks for async notifications, Polly (retry/circuit breaker) for resilient delivery, and API Key authentication.

**Use case:** Simulated gateway for integration testing, demos, or learning payment flows. Represents the kind of service a fintech or POS platform would integrate with to process payments.

**Tech:** .NET 8, EF Core SQLite, minimal APIs, API Key auth, webhooks (BackgroundService + Channel), Polly (Microsoft.Extensions.Http.Resilience).

### Cenários fintech/POS

| Cenário | Descrição |
|---------|-----------|
| **Integração POS** | Loja física envia pagamentos PIX/cartão; webhook notifica conclusão. |
| **E-commerce** | Checkout integra com gateway; Idempotency-Key evita cobrança duplicada em retries. |
| **SaaS de pagamentos** | API Key por merchant; cada merchant pode configurar webhookUrl para receber eventos. |

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PayGateway.Api                                                         │
│  (minimal APIs, /api/v1/*, API Key, ProblemDetails, Swagger)            │
│  WebhookDeliveryService (BackgroundService + Channel) → HttpClient+Polly│
└─────────────────────────────────────────────────────────────────────────┘
         │                                    │
         ▼                                    ▼
┌──────────────────────────┐     ┌─────────────────────┐
│ PayGateway.Infrastructure │────▶│  PayGateway.Domain  │
│  (EF Core, SQLite)        │     │  (Payment, enums)   │
└──────────────────────────┘     └─────────────────────┘
```

| Project | Description |
|---------|-------------|
| **PayGateway.Api** | Web API (.NET 8, minimal APIs). Endpoints, ProblemDetails, Swagger, API Key, versioning. WebhookDeliveryService (BackgroundService + Channel). |
| **PayGateway.Domain** | Domain entities: Payment, enums (PaymentMethod, PaymentStatus, PixKeyType, CardBrand). |
| **PayGateway.Infrastructure** | EF Core, PaymentDbContext, SQLite persistence, Fluent API configurations. |

### Domain model

| Entity | Description |
|--------|-------------|
| **Payment** | Payment record. IdempotencyKey for safe retries. Method: Pix (instant) or Card (simulated). Status: Pending, Processing, Completed, Failed, Refunded, Cancelled. |
| **PIX** | PixKey + PixKeyType (Email, Cpf, Phone, Random). Status Completed immediately. |
| **Card** | CardLast4 + CardBrand (Visa, Mastercard, Elo, Amex). Status Processing (simulated). |

## Prerequisites

- .NET 8 SDK

## How to run

```bash
dotnet run --project src/PayGateway.Api
```

Swagger UI: http://localhost:5000/swagger (or port shown in console).

## API (v1)

Base path: `/api/v1`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /health | Health check (public) |
| GET | /payments/{id} | Get payment by ID |
| POST | /payments | Create payment (idempotent; use `Idempotency-Key` header). Optional `webhookUrl` for callback. |

### Webhooks

When `webhookUrl` is provided in the request, the gateway POSTs a JSON payload when the payment is created. Resilience: Polly retry (3 attempts, exponential backoff) + circuit breaker (opens after 50% failure ratio, 30s break).

Payload example:
```json
{
  "eventType": "payment.created",
  "paymentId": "...",
  "merchantId": "merchant-123",
  "amount": 99.99,
  "currency": "BRL",
  "method": "Pix",
  "status": "Completed",
  "createdAtUtc": "2026-03-12T..."
}
```

### Authentication

All endpoints under `/api/v1` require the `X-API-KEY` header. The `/health` endpoint is public.

### Example: create PIX payment

```bash
curl -X POST http://localhost:5162/api/v1/payments \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: change-me" \
  -H "Idempotency-Key: pay-001" \
  -d '{"merchantId":"merchant-123","amount":99.99,"currency":"BRL","method":0,"pixKey":"user@example.com","pixKeyType":0}'
```

Method: 0 = Pix, 1 = Card. PixKeyType: 0 = Email, 1 = Cpf, 2 = Phone, 3 = Random.

### Example: create card payment

```bash
curl -X POST http://localhost:5162/api/v1/payments \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: change-me" \
  -H "Idempotency-Key: pay-002" \
  -d '{"merchantId":"merchant-123","amount":150.00,"currency":"BRL","method":1,"cardLast4":"4242","cardBrand":0}'
```

CardBrand: 0 = Visa, 1 = Mastercard, 2 = Elo, 3 = Amex.

## Technologies

- .NET 8
- ASP.NET Core minimal APIs
- Entity Framework Core 8 (SQLite)
- Swagger / OpenAPI
- Microsoft.Extensions.Http.Resilience (Polly: retry, circuit breaker)
- BackgroundService + Channel

## Configuration

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=paygateway.db"
  },
  "ApiKey": {
    "Value": "change-me"
  }
}
```

- **ApiKey:Value**: Required for authentication. All API endpoints (except `/health`) require the `X-API-KEY` header. Use a secure value in production.

## Status

**Etapa 8 concluída.** README completo com contexto fintech/POS, arquitetura, modelo de domínio e tecnologias.

See `portfolio-notes.md` for the roadmap and execution history.
