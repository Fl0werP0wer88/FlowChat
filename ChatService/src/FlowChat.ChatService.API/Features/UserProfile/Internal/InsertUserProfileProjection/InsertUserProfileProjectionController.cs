using FlowChat.ChatService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.InsertUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/insert")]
public sealed class InsertUserProfileProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public InsertUserProfileProjectionController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost]
    public async Task<IActionResult> Insert(
        [FromBody] UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new InsertUserProfileProjectionCommand(
                request.UserProfileId,
                request.FriendlyUserId,
                request.DisplayName,
                request.AvatarUrl,
                request.Source),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return result.Value.WasAlreadyProcessed
            ? Ok()
            : StatusCode(StatusCodes.Status201Created);
    }
}
