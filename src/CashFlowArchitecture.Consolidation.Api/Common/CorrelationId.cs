namespace CashFlowArchitecture.Consolidation.Api.Common;

internal static class CorrelationId
{
    private const string HeaderName = "X-Correlation-Id";

    public static string GetOrCreate(HttpContext httpContext)
    {
        var correlationId = httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        httpContext.Response.Headers[HeaderName] = correlationId;

        return correlationId;
    }
}
