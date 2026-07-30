using AutoMapper;
using FlowChat.GatewayService.Api.Configuration.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.GatewayService.Api.Features.Gateway.Public.GetGatewayRoutes;

[ApiController]
[Authorize]
[Route("gateway/routes")]
public sealed class GetGatewayRoutesController(
    IOptions<GatewayCatalogSettingsSection> catalogOptions,
    IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GetGatewayRoutesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<IReadOnlyCollection<GetGatewayRoutesResponse>> GetGatewayRoutes()
    {
        var response = mapper.Map<IReadOnlyCollection<GetGatewayRoutesResponse>>(
            catalogOptions.Value.Routes);
        return Ok(response);
    }
}
