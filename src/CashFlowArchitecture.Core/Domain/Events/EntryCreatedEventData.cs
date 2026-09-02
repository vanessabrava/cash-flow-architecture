using CashFlowArchitecture.Core.Domain.Entries;

namespace CashFlowArchitecture.Core.Domain.Events;

public sealed record EntryCreatedEventData(
    Guid EntryUid,
    EntryType Type,
    decimal Amount,
    DateOnly EntryDate);
