# PayGateway

Payment gateway simulation (PIX + card). Portfolio project aligned with Senior .NET Backend (fintech/POS) roles.

## Context

PayGateway is an API that simulates a **payment gateway** for processing PIX and card transactions. Planned features include webhooks for async notifications, retry/circuit breaker patterns (Polly), and background services for settlement.

**Use case:** Simulated gateway for integration testing, demos, or learning payment flows. Represents the kind of service a fintech or POS platform would integrate with to process payments.

**Planned tech:** .NET 8, Polly (retry/circuit breaker), background services, API Key/JWT authentication.

## Architecture

```
┌─────────────────┐     ┌──────────────────────────┐     ┌─────────────────────┐
│  PayGateway.Api │────▶│ PayGateway.Infrastructure │────▶│  PayGateway.Domain  │
│  (minimal APIs) │     │  (EF Core, SQLite)        │     │  (Payment, enums)   │
│  /api/v1/*      │     │  (future: Polly, etc.)    │     │                     │
└─────────────────┘     └──────────────────────────┘     └─────────────────────┘
```

| Project | Description |
|---------|-------------|
| **PayGateway.Api** | Web API (.NET 8, minimal APIs). Endpoints, Swagger, versioning. |
| **PayGateway.Domain** | Domain entities: Payment, enums (PaymentMethod, PaymentStatus, PixKeyType, CardBrand). |
| **PayGateway.Infrastructure** | EF Core, PaymentDbContext, SQLite persistence, Fluent API configurations. |

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
| POST | /payments | Create payment (idempotent; use `Idempotency-Key` header) |

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

**Etapa 5 concluída.** API Key authentication (X-API-KEY). Next: webhooks, Polly.

See `portfolio-notes.md` for the roadmap and execution history.
