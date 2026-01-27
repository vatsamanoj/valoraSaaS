using Lab360.Application.Common.Results;
using MediatR;
using Valora.Api.Domain.Entities.Finance;

namespace Valora.Api.Application.Finance.Queries.GetGLAccounts;

public record GetGLAccountsQuery(string TenantId) : IRequest<ApiResult>;
