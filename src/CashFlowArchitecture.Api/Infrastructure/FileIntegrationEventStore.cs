using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Api.Domain.Events;

namespace CashFlowArchitecture.Api.Infrastructure;

internal sealed class FileIntegrationEventStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string filePath;
    private readonly Lock syncRoot = new();

    public FileIntegrationEventStore(IHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:IntegrationEventsPath"];

        filePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "data", "integration-events.json")
            : GetFilePath(environment, configuredPath);
    }

    public void Add(EntryCreatedEvent integrationEvent)
    {
        lock (syncRoot)
        {
            var events = ReadAll();
            events.Add(integrationEvent);
            Save(events);
        }
    }

    private List<EntryCreatedEvent> ReadAll()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(filePath);

        return JsonSerializer.Deserialize<List<EntryCreatedEvent>>(stream, JsonOptions) ?? [];
    }

    private void Save(IReadOnlyCollection<EntryCreatedEvent> events)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, events, JsonOptions);
    }

    private static string GetFilePath(IHostEnvironment environment, string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }
}
