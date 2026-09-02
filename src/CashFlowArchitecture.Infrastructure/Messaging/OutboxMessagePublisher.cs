using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Core.Domain.Events;
using CashFlowArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CashFlowArchitecture.Infrastructure.Messaging;

public sealed class OutboxMessagePublisher(
    IServiceScopeFactory scopeFactory,
    RabbitMqIntegrationEventPublisher rabbitMqIntegrationEventPublisher,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<OutboxMessagePublisher> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly int batchSize = Math.Max(1, outboxOptions.Value.BatchSize);
    private readonly int maxRetryCount = Math.Max(1, outboxOptions.Value.MaxRetryCount);
    private readonly int retryDelaySeconds = Math.Max(1, outboxOptions.Value.RetryDelaySeconds);
    private readonly int publishIntervalSeconds = Math.Max(1, outboxOptions.Value.PublishIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(publishIntervalSeconds));

        await PublishPendingMessagesAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PublishPendingMessagesAsync(stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();
        var now = DateTimeOffset.UtcNow;
        var messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null
                && message.FailedAt == null
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(message.Payload, JsonOptions);

                if (integrationEvent is null)
                {
                    MarkAsFailed(message, "Payload invalido para publicacao.", maxRetryCount);
                    continue;
                }

                await rabbitMqIntegrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);

                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.NextAttemptAt = null;
                message.LastError = null;

                logger.LogInformation(
                    "Outbox message published. EventUid: {EventUid}. CorrelationId: {CorrelationId}.",
                    message.EventUid,
                    message.CorrelationId);
            }
            catch (Exception exception)
            {
                RegisterFailure(message, exception.Message, maxRetryCount, retryDelaySeconds);

                logger.LogWarning(
                    exception,
                    "Error publishing outbox message. EventUid: {EventUid}. CorrelationId: {CorrelationId}. RetryCount: {RetryCount}. Failed: {Failed}.",
                    message.EventUid,
                    message.CorrelationId,
                    message.RetryCount,
                    message.FailedAt is not null);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void RegisterFailure(
        OutboxMessageEntity message,
        string error,
        int maxRetryCount,
        int retryDelaySeconds)
    {
        message.RetryCount++;
        message.LastError = Truncate(error);

        if (message.RetryCount >= maxRetryCount)
        {
            message.FailedAt = DateTimeOffset.UtcNow;
            message.NextAttemptAt = null;
            return;
        }

        message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(retryDelaySeconds);
    }

    private static void MarkAsFailed(OutboxMessageEntity message, string error, int maxRetryCount)
    {
        message.RetryCount = maxRetryCount;
        message.FailedAt = DateTimeOffset.UtcNow;
        message.NextAttemptAt = null;
        message.LastError = Truncate(error);
    }

    private static string Truncate(string value)
    {
        return value.Length <= 1000 ? value : value[..1000];
    }
}
