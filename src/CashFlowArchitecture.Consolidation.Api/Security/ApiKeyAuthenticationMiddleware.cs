using System.Security.Cryptography;
using System.Text;
using CashFlowArchitecture.Consolidation.Api.Common;
using CashFlowArchitecture.Consolidation.Api.Contracts;

namespace CashFlowArchitecture.Consolidation.Api.Security;

internal sealed class ApiKeyAuthenticationMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate next;
    private readonly IConfiguration configuration;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        this.next = next;
        this.configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsPublicEndpoint(context.Request.Path))
        {
            await next(context);
            return;
        }

        var expectedApiKey = configuration["Authentication:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "AUTHENTICATION_NOT_CONFIGURED",
                "A autenticacao da API nao foi configurada.");
            return;
        }

        var providedApiKey = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedApiKey) || !Matches(providedApiKey, expectedApiKey))
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "AUTHENTICATION_REQUIRED",
                "Informe uma API Key valida para acessar este recurso.");
            return;
        }

        await next(context);
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        return path.StartsWithSegments("/health")
            || path.StartsWithSegments("/swagger");
    }

    private static bool Matches(string providedApiKey, string expectedApiKey)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);

        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        var correlationId = CorrelationId.GetOrCreate(context);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(
            correlationId,
            code,
            message,
            Array.Empty<ErrorDetail>()));
    }
}
