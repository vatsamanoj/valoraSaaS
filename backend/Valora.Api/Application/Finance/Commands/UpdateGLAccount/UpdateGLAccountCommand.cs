using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Finance.Commands.UpdateGLAccount;

public record UpdateGLAccountCommand(
    string TenantId,
    Guid Id,
    string Name,
    string Type,
    bool IsActive,
    string UpdatedBy
) : IRequest<ApiResult>;
