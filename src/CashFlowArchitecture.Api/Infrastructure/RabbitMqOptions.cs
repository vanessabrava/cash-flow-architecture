namespace CashFlowArchitecture.Api.Infrastructure;

internal sealed record RabbitMqOptions(
    string HostName,
    int Port,
    string UserName,
    string Password,
    string VirtualHost,
    string Exchange,
    string Queue,
    string RoutingKey)
{
    public static RabbitMqOptions From(IConfiguration configuration)
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
}
