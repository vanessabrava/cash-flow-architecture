using CashFlowArchitecture.Api.Domain.Entries;

namespace CashFlowArchitecture.Api.Domain.Events;

internal sealed record EntryCreatedEventData(
    Guid EntryUid,
    EntryType Type,
    decimal Amount,
    DateOnly EntryDate);
