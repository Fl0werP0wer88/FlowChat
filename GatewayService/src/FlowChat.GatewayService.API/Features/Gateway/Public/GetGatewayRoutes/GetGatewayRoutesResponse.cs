using FlowChat.Core.Contracts;

namespace FlowChat.GatewayService.Api.Features.Gateway.Public.GetGatewayRoutes;

public sealed record GetGatewayRoutesResponse(
    string Name,
    string PublicPath,
    string ClusterId,
    bool RequiresAuthentication) : IServiceOutput;
