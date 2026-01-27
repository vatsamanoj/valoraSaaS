using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Finance.Commands.CreateGLAccount;

public record CreateGLAccountCommand(
    string TenantId,
    string AccountCode,
    string Name,
    string Type,
    string UserId
) : IRequest<ApiResult>;
