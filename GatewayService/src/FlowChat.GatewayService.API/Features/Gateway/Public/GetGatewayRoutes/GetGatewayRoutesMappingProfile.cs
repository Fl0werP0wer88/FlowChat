using AutoMapper;
using FlowChat.GatewayService.Api.Configuration.Settings;

namespace FlowChat.GatewayService.Api.Features.Gateway.Public.GetGatewayRoutes;

public sealed class GetGatewayRoutesMappingProfile : Profile
{
    public GetGatewayRoutesMappingProfile()
    {
        CreateMap<GatewayRouteCatalogEntry, GetGatewayRoutesResponse>();
    }
}
