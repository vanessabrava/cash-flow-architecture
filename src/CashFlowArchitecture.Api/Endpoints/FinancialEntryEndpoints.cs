using CashFlowArchitecture.Api.Common;
using CashFlowArchitecture.Api.Contracts;
using CashFlowArchitecture.Api.Contracts.Entries;
using CashFlowArchitecture.Core.Domain.Entries;
using CashFlowArchitecture.Core.Domain.Events;
using CashFlowArchitecture.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CashFlowArchitecture.Api.Endpoints;

internal static class FinancialEntryEndpoints
{
    private const string CreateEntryOperation = "POST /entries";
    private const string IdempotencyKeyHeaderName = "Idempotency-Key";

    public static void MapFinancialEntryEndpoints(this WebApplication app)
    {
        var entries = app.MapGroup("/entries");

        entries.MapPost("/", Create)
            .WithName("CreateFinancialEntry")
            .WithSummary("Cria um lançamento financeiro.")
            .WithDescription("Registra um lançamento financeiro de crédito ou débito e retorna seu UID público.");

        entries.MapGet("/", GetByDate)
            .WithName("GetFinancialEntriesByDate")
            .WithSummary("Consulta lançamentos por data.")
            .WithDescription("Retorna os lançamentos financeiros registrados para a data informada.");
    }

    private static async Task<IResult> Create(
        CreateFinancialEntryRequest request,
        HttpContext httpContext,
        IFinancialEntryStore store,
        IIdempotencyStore idempotencyStore,
        IIntegrationEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        [FromHeader(Name = IdempotencyKeyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var correlationId = CorrelationId.GetOrCreate(httpContext);
        var validationErrors = Validate(request);
        idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim();

        if (validationErrors.Count > 0)
        {
            return Results.BadRequest(new ErrorResponse(
                correlationId,
                "VALIDATION_ERROR",
                "A requisicao possui campos invalidos.",
                validationErrors));
        }

        if (idempotencyKey is { Length: > 200 })
        {
            return Results.BadRequest(new ErrorResponse(
                correlationId,
                "VALIDATION_ERROR",
                "A requisicao possui campos invalidos.",
                [new ErrorDetail(IdempotencyKeyHeaderName, "A chave de idempotencia deve ter no maximo 200 caracteres.")]));
        }

        var requestHash = ComputeRequestHash(request);

        if (idempotencyKey is not null)
        {
            var existingRecord = idempotencyStore.Get(CreateEntryOperation, idempotencyKey);

            if (existingRecord is not null)
            {
                if (existingRecord.RequestHash != requestHash)
                {
                    return Results.Conflict(new ErrorResponse(
                        correlationId,
                        "IDEMPOTENCY_KEY_CONFLICT",
                        "A Idempotency-Key informada ja foi usada com outro conteudo.",
                        [new ErrorDetail(IdempotencyKeyHeaderName, "Use uma nova chave para uma nova tentativa logica de criacao.")]));
                }

                var existingEntry = store.GetByUid(existingRecord.ResourceUid);

                if (existingEntry is not null)
                {
                    return Results.Ok(ToResponse(correlationId, existingEntry));
                }
            }
        }

        var entry = new FinancialEntry(
            Guid.NewGuid(),
            request.Type,
            request.Amount,
            request.Description.Trim(),
            request.EntryDate,
            DateTimeOffset.UtcNow);

        store.Add(entry);
        await eventPublisher.PublishAsync(EntryCreatedEvent.From(entry, correlationId), cancellationToken);

        if (idempotencyKey is not null)
        {
            idempotencyStore.Add(new IdempotencyRecord(
                CreateEntryOperation,
                idempotencyKey,
                requestHash,
                entry.Uid,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(24)));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Created($"/entries/{entry.Uid}", ToResponse(correlationId, entry));
    }

    private static IResult GetByDate(
        DateOnly date,
        HttpContext httpContext,
        IFinancialEntryStore store)
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

    private static string ComputeRequestHash(CreateFinancialEntryRequest request)
    {
        var normalizedRequest = string.Join(
            '|',
            request.Type.ToString(),
            request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            request.Description.Trim(),
            request.EntryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRequest));

        return Convert.ToHexString(bytes);
    }

    private static FinancialEntryResponse ToResponse(string correlationId, FinancialEntry entry)
    {
        return new FinancialEntryResponse(
            correlationId,
            entry.Uid,
            entry.Type,
            entry.Amount,
            entry.Description,
            entry.EntryDate,
            entry.CreatedAt);
    }
}
