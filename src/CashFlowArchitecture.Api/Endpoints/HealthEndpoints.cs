using CashFlowArchitecture.Api.Common;
using CashFlowArchitecture.Api.Contracts.Health;
using CashFlowArchitecture.Infrastructure.Messaging;
using CashFlowArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;

namespace CashFlowArchitecture.Api.Endpoints;

internal static class HealthEndpoints
{
    private const string ServiceName = "cash-flow-api";

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
        .WithSummary("Consulta a saúde básica da API.")
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
        .WithSummary("Consulta o liveness da API.")
        .WithDescription("Indica se o processo da API está vivo.");

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
                dependencies.Add(await CheckRabbitMqAsync(configuration, cancellationToken));
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
        .WithSummary("Consulta o readiness da API.")
        .WithDescription("Indica se a API está pronta para operar com suas dependências principais.");
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
                "health:ready",
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

    private static async Task<DependencyHealthResponse> CheckRabbitMqAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = RabbitMqOptions.From(configuration);
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost
            };

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            await using var connection = await factory.CreateConnectionAsync(timeout.Token);
            return Healthy("rabbitmq", critical: false);
        }
        catch (Exception exception)
        {
            return Unhealthy("rabbitmq", critical: false, exception.Message);
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
