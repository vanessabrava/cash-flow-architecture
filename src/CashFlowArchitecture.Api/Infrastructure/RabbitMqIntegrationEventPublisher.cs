using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Api.Domain.Events;
using RabbitMQ.Client;

namespace CashFlowArchitecture.Api.Infrastructure;

internal sealed class RabbitMqIntegrationEventPublisher(IConfiguration configuration) : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task PublishAsync(EntryCreatedEvent integrationEvent, CancellationToken cancellationToken)
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

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            options.Exchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            options.Queue,
            options.Exchange,
            options.RoutingKey,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent, JsonOptions));
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            CorrelationId = integrationEvent.CorrelationId,
            MessageId = integrationEvent.EventUid.ToString(),
            Persistent = true
        };

        await channel.BasicPublishAsync(
            options.Exchange,
            options.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
