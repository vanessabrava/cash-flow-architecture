using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.DailyBalances;
using CashFlowArchitecture.Core.Domain.Entries;
using CashFlowArchitecture.Core.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CashFlowArchitecture.Infrastructure.Storage;

public sealed class FileDailyBalanceStore : IDailyBalanceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string filePath;
    private readonly Lock syncRoot = new();

    public FileDailyBalanceStore(IHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:DailyBalancesPath"];

        filePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "data", "daily-balances.json")
            : GetFilePath(environment, configuredPath);
    }

    public DailyBalance? GetByDate(DateOnly balanceDate)
    {
        lock (syncRoot)
        {
            return ReadAll().SingleOrDefault(balance => balance.BalanceDate == balanceDate);
        }
    }

    public bool Apply(EntryCreatedEvent integrationEvent)
    {
        lock (syncRoot)
        {
            var balances = ReadAll();
            var currentBalance = balances.SingleOrDefault(balance =>
                balance.BalanceDate == integrationEvent.Data.EntryDate);

            if (currentBalance?.ProcessedEventUids.Contains(integrationEvent.EventUid) == true)
            {
                return false;
            }

            var processedEventUids = currentBalance?.ProcessedEventUids.ToList() ?? [];
            processedEventUids.Add(integrationEvent.EventUid);

            var totalCredits = currentBalance?.TotalCredits ?? 0;
            var totalDebits = currentBalance?.TotalDebits ?? 0;

            if (integrationEvent.Data.Type == EntryType.CREDIT)
            {
                totalCredits += integrationEvent.Data.Amount;
            }
            else
            {
                totalDebits += integrationEvent.Data.Amount;
            }

            var updatedBalance = new DailyBalance(
                currentBalance?.Uid ?? Guid.NewGuid(),
                integrationEvent.Data.EntryDate,
                totalCredits,
                totalDebits,
                "CONSOLIDATED",
                DateTimeOffset.UtcNow,
                processedEventUids);

            balances.RemoveAll(balance => balance.BalanceDate == integrationEvent.Data.EntryDate);
            balances.Add(updatedBalance);
            Save(balances.OrderBy(balance => balance.BalanceDate).ToArray());

            return true;
        }
    }

    private List<DailyBalance> ReadAll()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(filePath);

        return JsonSerializer.Deserialize<List<DailyBalance>>(stream, JsonOptions) ?? [];
    }

    private void Save(IReadOnlyCollection<DailyBalance> balances)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, balances, JsonOptions);
    }

    private static string GetFilePath(IHostEnvironment environment, string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }
}
