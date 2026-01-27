using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Finance.Queries.GetJournalEntries;

public record GetJournalEntriesQuery(string TenantId, int Page = 1, int PageSize = 20, string? DocumentNumber = null, DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<ApiResult>;
