namespace CashFlowArchitecture.Consolidation.Api.Contracts.Health;

public sealed record DependencyHealthResponse(
    string Name,
    string Status,
    bool Critical,
    string? Message);
