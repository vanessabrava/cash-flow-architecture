using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Api.Domain.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CashFlowArchitecture.Api.Infrastructure;

internal sealed class RabbitMqDailyBalanceWorker(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqDailyBalanceWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            options.Exchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            options.Queue,
            options.Exchange,
            options.RoutingKey,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<EntryCreatedEvent>(args.Body.Span, JsonOptions);

                if (integrationEvent is null)
                {
                    await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    return;
                }

                using var scope = scopeFactory.CreateScope();
                var dailyBalanceStore = scope.ServiceProvider.GetRequiredService<IDailyBalanceStore>();
                var processed = dailyBalanceStore.Apply(integrationEvent);

                logger.LogInformation(
                    "EntryCreated event consumed. EventUid: {EventUid}. CorrelationId: {CorrelationId}. Processed: {Processed}.",
                    integrationEvent.EventUid,
                    integrationEvent.CorrelationId,
                    processed);

                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error consuming EntryCreated event.");
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(options.Queue, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Daily balance worker listening queue {Queue}.", options.Queue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
