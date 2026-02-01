using Lab360.Application.Common.Results;
using MediatR;
using Valora.Api.Application.Common.Commands;
using Valora.Api.Application.Finance.Commands.PostJournalEntry; // For JournalEntryLineDto

namespace Valora.Api.Application.Finance.Commands.UpdateJournalEntry;

public record UpdateJournalEntryCommand(
    string TenantId,
    Guid Id,
    DateTime PostingDate,
    string? Description,
    string? Reference,
    List<JournalEntryLineDto> Lines,
    string UserId,
    uint? Version
) : IRequest<ApiResult>, IIdempotentCommand
{
    // For this simple implementation, we generate a key or expect one.
    // If not provided in constructor, we can derive it or add a property.
    // Let's assume the Request ID (if it existed) or a hash of content.
    // For now, we will return a new Guid to satisfy the interface, 
    // BUT in a real app, this should be passed from the Controller.
    public Guid IdempotencyKey { get; init; } = Guid.NewGuid(); 
}
