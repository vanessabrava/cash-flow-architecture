using System.Text.Json;
using System.Text.Json.Serialization;
using CashFlowArchitecture.Core.Abstractions;
using CashFlowArchitecture.Core.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CashFlowArchitecture.Infrastructure.Storage;

public sealed class FileIntegrationEventStore : IIntegrationEventPublisher
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

    public Task PublishAsync(EntryCreatedEvent integrationEvent, CancellationToken cancellationToken)
    {
        Add(integrationEvent);

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<EntryCreatedEvent> GetAll()
    {
        lock (syncRoot)
        {
            return ReadAll()
                .OrderBy(integrationEvent => integrationEvent.OccurredAt)
                .ToArray();
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
