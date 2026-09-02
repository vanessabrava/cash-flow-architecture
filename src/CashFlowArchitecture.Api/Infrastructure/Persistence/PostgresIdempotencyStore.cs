namespace CashFlowArchitecture.Api.Infrastructure.Persistence;

internal sealed class PostgresIdempotencyStore(CashFlowDbContext dbContext) : IIdempotencyStore
{
    public IdempotencyRecord? Get(string operation, string key)
    {
        var now = DateTimeOffset.UtcNow;

        return dbContext.IdempotencyRecords
            .Where(record => record.Operation == operation
                && record.Key == key
                && record.ExpiresAt > now)
            .Select(record => new IdempotencyRecord(
                record.Operation,
                record.Key,
                record.RequestHash,
                record.ResourceUid,
                record.CreatedAt,
                record.ExpiresAt))
            .SingleOrDefault();
    }

    public void Add(IdempotencyRecord record)
    {
        dbContext.IdempotencyRecords.Add(new IdempotencyRecordEntity
        {
            Operation = record.Operation,
            Key = record.Key,
            RequestHash = record.RequestHash,
            ResourceUid = record.ResourceUid,
            CreatedAt = record.CreatedAt,
            ExpiresAt = record.ExpiresAt
        });

        dbContext.SaveChanges();
    }
}
