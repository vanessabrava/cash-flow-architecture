using System.Text.Json;
using CashFlowArchitecture.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CashFlowArchitecture.Infrastructure.Storage;

public sealed class FileIdempotencyStore : IIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string filePath;
    private readonly Lock syncRoot = new();

    public FileIdempotencyStore(IHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:IdempotencyRecordsPath"];

        filePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "data", "idempotency-records.json")
            : GetFilePath(environment, configuredPath);
    }

    public IdempotencyRecord? Get(string operation, string key)
    {
        lock (syncRoot)
        {
            var now = DateTimeOffset.UtcNow;

            return ReadAll()
                .Where(record => record.ExpiresAt > now)
                .SingleOrDefault(record => record.Operation == operation && record.Key == key);
        }
    }

    public void Add(IdempotencyRecord record)
    {
        lock (syncRoot)
        {
            var records = ReadAll()
                .Where(existingRecord => existingRecord.ExpiresAt > DateTimeOffset.UtcNow)
                .ToList();

            records.RemoveAll(existingRecord =>
                existingRecord.Operation == record.Operation && existingRecord.Key == record.Key);
            records.Add(record);
            Save(records);
        }
    }

    private List<IdempotencyRecord> ReadAll()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(filePath);

        return JsonSerializer.Deserialize<List<IdempotencyRecord>>(stream, JsonOptions) ?? [];
    }

    private void Save(IReadOnlyCollection<IdempotencyRecord> records)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, records, JsonOptions);
    }

    private static string GetFilePath(IHostEnvironment environment, string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }
}
