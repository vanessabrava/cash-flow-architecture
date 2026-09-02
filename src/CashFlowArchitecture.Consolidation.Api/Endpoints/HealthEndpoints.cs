using CashFlowArchitecture.Consolidation.Api.Common;
using CashFlowArchitecture.Consolidation.Api.Contracts.Health;
using CashFlowArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CashFlowArchitecture.Consolidation.Api.Endpoints;

internal static class HealthEndpoints
{
    private const string ServiceName = "cash-flow-consolidation-api";

    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", (HttpContext httpContext) =>
        {
            var response = new HealthResponse(
                CorrelationId.GetOrCreate(httpContext),
                "Healthy",
                ServiceName,
                DateTimeOffset.UtcNow,
                []);

            return Results.Ok(response);
        })
        .WithName("GetHealth")
        .WithSummary("Consulta a saúde básica da API de consolidação.")
        .WithDescription("Retorna o estado básico de disponibilidade da aplicação.");

        app.MapGet("/health/live", (HttpContext httpContext) =>
        {
            var response = new HealthResponse(
                CorrelationId.GetOrCreate(httpContext),
                "Healthy",
                ServiceName,
                DateTimeOffset.UtcNow,
                []);

            return Results.Ok(response);
        })
        .WithName("GetLiveness")
        .WithSummary("Consulta o liveness da API de consolidação.")
        .WithDescription("Indica se o processo da API de consolidação está vivo.");

        app.MapGet("/health/ready", async (
            HttpContext httpContext,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var dependencies = new List<DependencyHealthResponse>();

            if (environment.IsEnvironment("Testing")
                || configuration.GetValue("Storage:UseFileStorage", false))
            {
                dependencies.Add(new DependencyHealthResponse(
                    "local-storage",
                    "Healthy",
                    true,
                    "Armazenamento local em arquivo configurado."));
            }
            else
            {
                dependencies.Add(await CheckPostgresAsync(serviceProvider, cancellationToken));
                dependencies.Add(await CheckRedisAsync(serviceProvider, cancellationToken));
            }

            var status = GetOverallStatus(dependencies);
            var response = new HealthResponse(
                CorrelationId.GetOrCreate(httpContext),
                status,
                ServiceName,
                DateTimeOffset.UtcNow,
                dependencies);

            return HasCriticalFailure(dependencies)
                ? Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable)
                : Results.Ok(response);
        })
        .WithName("GetReadiness")
        .WithSummary("Consulta o readiness da API de consolidação.")
        .WithDescription("Indica se a API de consolidação está pronta para consultar saldos.");
    }

    private static async Task<DependencyHealthResponse> CheckPostgresAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var dbContext = serviceProvider.GetRequiredService<CashFlowDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? Healthy("postgres", critical: true)
                : Unhealthy("postgres", critical: true, "Conexao indisponivel.");
        }
        catch (Exception exception)
        {
            return Unhealthy("postgres", critical: true, exception.Message);
        }
    }

    private static async Task<DependencyHealthResponse> CheckRedisAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var cache = serviceProvider.GetRequiredService<IDistributedCache>();
            await cache.SetStringAsync(
                "health:ready:consolidation-api",
                DateTimeOffset.UtcNow.ToString("O"),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                },
                cancellationToken);

            return Healthy("redis", critical: false);
        }
        catch (Exception exception)
        {
            return Unhealthy("redis", critical: false, exception.Message);
        }
    }

    private static DependencyHealthResponse Healthy(string name, bool critical)
    {
        return new DependencyHealthResponse(name, "Healthy", critical, null);
    }

    private static DependencyHealthResponse Unhealthy(string name, bool critical, string message)
    {
        return new DependencyHealthResponse(name, "Unhealthy", critical, message);
    }

    private static bool HasCriticalFailure(IEnumerable<DependencyHealthResponse> dependencies)
    {
        return dependencies.Any(dependency => dependency.Critical && dependency.Status != "Healthy");
    }

    private static string GetOverallStatus(IReadOnlyCollection<DependencyHealthResponse> dependencies)
    {
        if (HasCriticalFailure(dependencies))
        {
            return "Unhealthy";
        }

        return dependencies.Any(dependency => dependency.Status != "Healthy")
            ? "Degraded"
            : "Healthy";
    }
}
