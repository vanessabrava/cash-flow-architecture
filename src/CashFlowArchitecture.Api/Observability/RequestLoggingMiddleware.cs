using System.Diagnostics;
using CashFlowArchitecture.Api.Common;

namespace CashFlowArchitecture.Api.Observability;

internal sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestLoggingMiddleware> logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationId.GetOrCreate(context);
        context.TraceIdentifier = correlationId;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        var stopwatch = Stopwatch.StartNew();
        var failed = false;

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            failed = true;
            stopwatch.Stop();
            logger.LogError(
                exception,
                "HTTP request failed. Method: {Method}. Path: {Path}. StatusCode: {StatusCode}. DurationMs: {DurationMs}. CorrelationId: {CorrelationId}.",
                context.Request.Method,
                context.Request.Path.Value,
                StatusCodes.Status500InternalServerError,
                stopwatch.ElapsedMilliseconds,
                correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (!failed)
            {
                logger.LogInformation(
                    "HTTP request completed. Method: {Method}. Path: {Path}. StatusCode: {StatusCode}. DurationMs: {DurationMs}. CorrelationId: {CorrelationId}.",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    correlationId);
            }
        }
    }
}
