using CashFlowArchitecture.Api.Common;
using CashFlowArchitecture.Api.Contracts;
using CashFlowArchitecture.Api.Contracts.Entries;
using CashFlowArchitecture.Api.Domain.Entries;
using CashFlowArchitecture.Api.Infrastructure;

namespace CashFlowArchitecture.Api.Endpoints;

internal static class FinancialEntryEndpoints
{
    public static void MapFinancialEntryEndpoints(this WebApplication app)
    {
        var entries = app.MapGroup("/entries");

        entries.MapPost("/", Create);
        entries.MapGet("/", GetByDate);
    }

    private static IResult Create(
        CreateFinancialEntryRequest request,
        HttpContext httpContext,
        FileFinancialEntryStore store)
    {
        var correlationId = CorrelationId.GetOrCreate(httpContext);
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return Results.BadRequest(new ErrorResponse(
                correlationId,
                "VALIDATION_ERROR",
                "A requisicao possui campos invalidos.",
                validationErrors));
        }

        var entry = new FinancialEntry(
            Guid.NewGuid(),
            request.Type,
            request.Amount,
            request.Description.Trim(),
            request.EntryDate,
            DateTimeOffset.UtcNow);

        store.Add(entry);

        var response = new FinancialEntryResponse(
            correlationId,
            entry.Uid,
            entry.Type,
            entry.Amount,
            entry.Description,
            entry.EntryDate,
            entry.CreatedAt);

        return Results.Created($"/entries/{entry.Uid}", response);
    }

    private static IResult GetByDate(
        DateOnly date,
        HttpContext httpContext,
        FileFinancialEntryStore store)
    {
        var correlationId = CorrelationId.GetOrCreate(httpContext);
        var items = store.GetByDate(date)
            .Select(entry => new FinancialEntryItemResponse(
                entry.Uid,
                entry.Type,
                entry.Amount,
                entry.Description,
                entry.EntryDate,
                entry.CreatedAt))
            .ToArray();

        return Results.Ok(new FinancialEntriesByDateResponse(correlationId, date, items));
    }

    private static List<ErrorDetail> Validate(CreateFinancialEntryRequest request)
    {
        var errors = new List<ErrorDetail>();

        if (request.Type is not EntryType.CREDIT and not EntryType.DEBIT)
        {
            errors.Add(new ErrorDetail("type", "O tipo deve ser CREDIT ou DEBIT."));
        }

        if (request.Amount <= 0)
        {
            errors.Add(new ErrorDetail("amount", "O valor deve ser maior que zero."));
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add(new ErrorDetail("description", "A descricao e obrigatoria."));
        }

        return errors;
    }
}
