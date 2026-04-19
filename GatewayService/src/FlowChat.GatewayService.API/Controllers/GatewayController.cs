using FlowChat.GatewayService.Api.Configuration.Settings;
using FlowChat.GatewayService.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.GatewayService.Api.Controllers;

[ApiController]
[Route("gateway")]
public sealed class GatewayController(
    IWebHostEnvironment environment,
    IOptions<GatewayCatalogSettingsSection> catalogOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("health")]
    [ProducesResponseType(typeof(GatewayStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<GatewayStatusResponse> GetHealth()
    {
        return Ok(new GatewayStatusResponse
        {
            Service = "FlowChat.GatewayService.API",
            Environment = environment.EnvironmentName,
            AuthenticationMode = "JWT Bearer",
            UtcNow = DateTimeOffset.UtcNow,
            Status = "Healthy"
        });
    }

    [HttpGet("routes")]
    [ProducesResponseType(typeof(IEnumerable<GatewayRouteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<IEnumerable<GatewayRouteResponse>> GetRoutes()
    {
        var routes = catalogOptions.Value.Routes
            .Select(route => new GatewayRouteResponse
            {
                Name = route.Name,
                PublicPath = route.PublicPath,
                ClusterId = route.ClusterId,
                RequiresAuthentication = route.RequiresAuthentication
            })
            .ToArray();

        return Ok(routes);
    }
}
