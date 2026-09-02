namespace CashFlowArchitecture.Infrastructure.Messaging;

public sealed class OutboxOptions
{
    public int BatchSize { get; set; } = 20;

    public int MaxRetryCount { get; set; } = 5;

    public int RetryDelaySeconds { get; set; } = 30;

    public int PublishIntervalSeconds { get; set; } = 5;
}
