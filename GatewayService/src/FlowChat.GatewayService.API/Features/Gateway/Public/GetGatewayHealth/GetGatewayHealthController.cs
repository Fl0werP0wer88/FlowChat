using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Features.Gateway.Public.GetGatewayHealth;

[ApiController]
[Route("gateway/health")]
public sealed class GetGatewayHealthController(IWebHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(GetGatewayHealthResponse), StatusCodes.Status200OK)]
    public ActionResult<GetGatewayHealthResponse> GetGatewayHealth() =>
        Ok(new GetGatewayHealthResponse(
            "FlowChat.GatewayService.API",
            environment.EnvironmentName,
            "JWT Bearer",
            DateTimeOffset.UtcNow,
            "Healthy"));
}
