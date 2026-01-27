using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Finance.Commands.PostJournalEntry;

public record JournalEntryLineDto(
    Guid GLAccountId,
    decimal Debit,
    decimal Credit,
    string? Description
);

public record PostJournalEntryCommand(
    string TenantId,
    DateTime PostingDate,
    string DocumentNumber, // Optional, usually generated
    string Description,
    string Reference,
    List<JournalEntryLineDto> Lines,
    string UserId
) : IRequest<ApiResult>;
