using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Features.Gateway.Public.GetGatewayHealth;

public sealed record GetGatewayHealthResponse(
    string Service,
    string Environment,
    string AuthenticationMode,
    DateTimeOffset UtcNow,
    string Status) : IServiceOutput;
