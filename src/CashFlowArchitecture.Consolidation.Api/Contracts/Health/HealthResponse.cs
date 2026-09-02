namespace CashFlowArchitecture.Consolidation.Api.Contracts.Health;

public sealed record HealthResponse(
    string CorrelationId,
    string Status,
    string Service,
    DateTimeOffset CheckedAt,
    IReadOnlyCollection<DependencyHealthResponse> Dependencies);
