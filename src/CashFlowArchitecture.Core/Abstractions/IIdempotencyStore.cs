namespace CashFlowArchitecture.Core.Abstractions;

public interface IIdempotencyStore
{
    IdempotencyRecord? Get(string operation, string key);

    void Add(IdempotencyRecord record);
}

public sealed record IdempotencyRecord(
    string Operation,
    string Key,
    string RequestHash,
    Guid ResourceUid,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
