using System.Text.Json;
using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Dynamic.Commands.UpdateEntity;

public record UpdateEntityCommand(
    string TenantId,
    string Module,
    string Id,
    JsonElement Body,
    string UserId = "system") : IRequest<ApiResult>;
