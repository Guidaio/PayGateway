using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PayGateway.Api.Swagger;

/// <summary>
/// Adds request/response examples to Swagger for PayGateway endpoints.
/// </summary>
public class SwaggerExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var operationId = operation.OperationId ?? "";
        var path = context.ApiDescription.RelativePath ?? "";

        if (operationId.Contains("GetPaymentById") || path.Contains("payments") && context.ApiDescription.HttpMethod == "GET")
        {
            operation.Summary = "Get payment by ID";
            operation.Description = "Returns a single payment by its GUID. Returns 404 if not found.";
        }
        else if (operationId.Contains("CreatePayment") || path.Contains("payments") && context.ApiDescription.HttpMethod == "POST")
        {
            operation.Summary = "Create payment (idempotent)";
            operation.Description = "Creates a PIX or card payment. Use Idempotency-Key header to prevent duplicates on retry. PIX returns Completed immediately; Card returns Processing.";
            if (operation.RequestBody?.Content.TryGetValue("application/json", out var mediaType) == true)
            {
                mediaType.Example = new OpenApiObject
                {
                    ["merchantId"] = new OpenApiString("merchant-123"),
                    ["amount"] = new OpenApiDouble(99.99),
                    ["currency"] = new OpenApiString("BRL"),
                    ["method"] = new OpenApiInteger(0),
                    ["pixKey"] = new OpenApiString("user@example.com"),
                    ["pixKeyType"] = new OpenApiInteger(0)
                };
            }
        }
    }
}
