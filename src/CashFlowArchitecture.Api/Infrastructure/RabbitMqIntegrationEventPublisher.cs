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
        var options = GetOptions();
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

    private RabbitMqOptions GetOptions()
    {
        return new RabbitMqOptions(
            configuration["RabbitMq:HostName"] ?? "localhost",
            configuration.GetValue("RabbitMq:Port", 5672),
            configuration["RabbitMq:UserName"] ?? "cash_flow_user",
            configuration["RabbitMq:Password"] ?? "cash_flow_password",
            configuration["RabbitMq:VirtualHost"] ?? "/",
            configuration["RabbitMq:Exchange"] ?? "cash-flow.events",
            configuration["RabbitMq:Queue"] ?? "cash-flow.entry-created",
            configuration["RabbitMq:RoutingKey"] ?? "entry.created");
    }

    private sealed record RabbitMqOptions(
        string HostName,
        int Port,
        string UserName,
        string Password,
        string VirtualHost,
        string Exchange,
        string Queue,
        string RoutingKey);
}
