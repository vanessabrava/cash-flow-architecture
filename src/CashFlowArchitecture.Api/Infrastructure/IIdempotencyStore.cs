namespace CashFlowArchitecture.Api.Infrastructure;

internal interface IIdempotencyStore
{
    IdempotencyRecord? Get(string operation, string key);

    void Add(IdempotencyRecord record);
}

internal sealed record IdempotencyRecord(
    string Operation,
    string Key,
    string RequestHash,
    Guid ResourceUid,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
