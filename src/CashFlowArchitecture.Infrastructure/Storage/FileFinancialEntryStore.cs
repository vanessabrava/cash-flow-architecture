using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.Entries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CashFlowArchitecture.Infrastructure.Storage;

public sealed class FileFinancialEntryStore : IFinancialEntryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string filePath;
    private readonly Lock syncRoot = new();

    public FileFinancialEntryStore(IHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:FinancialEntriesPath"];

        filePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "data", "financial-entries.json")
            : GetFilePath(environment, configuredPath);
    }

    public void Add(FinancialEntry entry)
    {
        lock (syncRoot)
        {
            var entries = ReadAll();
            entries.Add(entry);
            Save(entries);
        }
    }

    public IReadOnlyCollection<FinancialEntry> GetByDate(DateOnly entryDate)
    {
        lock (syncRoot)
        {
            return ReadAll()
                .Where(entry => entry.EntryDate == entryDate)
                .OrderBy(entry => entry.CreatedAt)
                .ToArray();
        }
    }

    public FinancialEntry? GetByUid(Guid uid)
    {
        lock (syncRoot)
        {
            return ReadAll().SingleOrDefault(entry => entry.Uid == uid);
        }
    }

    private List<FinancialEntry> ReadAll()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(filePath);

        return JsonSerializer.Deserialize<List<FinancialEntry>>(stream, JsonOptions) ?? [];
    }

    private void Save(IReadOnlyCollection<FinancialEntry> entries)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, entries, JsonOptions);
    }

    private static string GetFilePath(IHostEnvironment environment, string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }
}
