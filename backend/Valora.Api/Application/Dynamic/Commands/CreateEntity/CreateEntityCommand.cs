using System.Text.Json;
using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Dynamic.Commands.CreateEntity;

public record CreateEntityCommand(
    string TenantId,
    string Module,
    JsonElement Body,
    string UserId = "system") : IRequest<ApiResult>;
