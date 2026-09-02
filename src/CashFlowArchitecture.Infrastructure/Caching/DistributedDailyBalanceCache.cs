using System.Text.Json;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.DailyBalances;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CashFlowArchitecture.Infrastructure.Caching;

public sealed class DistributedDailyBalanceCache(
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<DistributedDailyBalanceCache> logger) : IDailyBalanceCache
{
    private readonly DistributedCacheEntryOptions cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
            Math.Max(1, configuration.GetValue("Redis:DailyBalanceTtlMinutes", 15)))
    };

    public DailyBalance? GetByDate(DateOnly balanceDate)
    {
        try
        {
            var cachedValue = cache.GetString(GetKey(balanceDate));

            return string.IsNullOrWhiteSpace(cachedValue)
                ? null
                : JsonSerializer.Deserialize<CachedDailyBalance>(cachedValue)?.ToDomain();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read daily balance from cache.");
            return null;
        }
    }

    public void Set(DailyBalance balance)
    {
        try
        {
            cache.SetString(
                GetKey(balance.BalanceDate),
                JsonSerializer.Serialize(CachedDailyBalance.From(balance)),
                cacheOptions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to write daily balance to cache.");
        }
    }

    private static string GetKey(DateOnly balanceDate)
    {
        return $"daily-balance:{balanceDate:yyyy-MM-dd}";
    }

    private sealed record CachedDailyBalance(
        Guid Uid,
        DateOnly Date,
        decimal TotalCredits,
        decimal TotalDebits,
        string Status,
        DateTimeOffset UpdatedAt)
    {
        public static CachedDailyBalance From(DailyBalance balance)
        {
            return new CachedDailyBalance(
                balance.Uid,
                balance.BalanceDate,
                balance.TotalCredits,
                balance.TotalDebits,
                balance.Status,
                balance.UpdatedAt);
        }

        public DailyBalance ToDomain()
        {
            return new DailyBalance(
                Uid,
                Date,
                TotalCredits,
                TotalDebits,
                Status,
                UpdatedAt,
                []);
        }
    }
}
