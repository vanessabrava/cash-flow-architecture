using CashFlowArchitecture.Api.Domain.Entries;

namespace CashFlowArchitecture.Api.Domain.Events;

internal sealed record EntryCreatedEvent(
    Guid EventUid,
    string CorrelationId,
    string EventType,
    DateTimeOffset OccurredAt,
    EntryCreatedEventData Data)
{
    public const string Type = "EntryCreated";

    public static EntryCreatedEvent From(FinancialEntry entry, string correlationId)
    {
        return new EntryCreatedEvent(
            Guid.NewGuid(),
            correlationId,
            Type,
            DateTimeOffset.UtcNow,
            new EntryCreatedEventData(
                entry.Uid,
                entry.Type,
                entry.Amount,
                entry.EntryDate));
    }
}
