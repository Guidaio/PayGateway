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

Base path: `/api/v1` (to be added).

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /health | Health check (public) |

## Configuration

Connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=paygateway.db"
  }
}
```

## Status

**Etapa 2 concluída.** Domain model (Payment, PIX/Card enums) + EF Core + SQLite. Next: POST /payments, GET /payments/{id}.

See `portfolio-notes.md` for the roadmap and execution history.
