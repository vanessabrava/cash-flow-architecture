namespace CashFlowArchitecture.Api.Contracts;

internal sealed record ErrorResponse(
    string CorrelationId,
    string Code,
    string Message,
    IReadOnlyCollection<ErrorDetail> Details);
