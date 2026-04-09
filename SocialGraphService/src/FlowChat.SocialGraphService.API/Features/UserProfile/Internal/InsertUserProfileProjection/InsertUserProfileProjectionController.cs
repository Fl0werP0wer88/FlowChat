using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.InsertUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/insert")]
public sealed class InsertUserProfileProjectionController(
    IMediator mediator,
    IApiSettingsManager apiSettingsManager)
    : InternalUserProfileProjectionControllerBase(apiSettingsManager)
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

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
                request.MainEmail,
                request.MainPhone,
                request.AvatarUrl,
                request.Bio,
                request.IsActive,
                request.LastSeenAtUtc,
                request.FirstName,
                request.LastName,
                request.Organization),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
