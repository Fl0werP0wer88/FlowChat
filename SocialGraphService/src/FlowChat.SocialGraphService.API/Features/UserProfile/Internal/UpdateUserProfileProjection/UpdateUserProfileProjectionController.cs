using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UpdateUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/update")]
public sealed class UpdateUserProfileProjectionController(
    IMediator mediator,
    IApiSettingsManager apiSettingsManager)
    : InternalUserProfileProjectionControllerBase(apiSettingsManager)
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new UpdateUserProfileProjectionCommand(
                request.UserProfileId,
                request.FriendlyUserId,
                request.DisplayName,
                request.MainEmail,
                request.MainPhone,
                request.AvatarUrl,
                request.Bio,
                request.IsActive,
                request.LastSeenAtUtc,
                request.IsEmailVisible,
                request.IsPhoneVisible,
                request.FirstName,
                request.LastName,
                request.Organization),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
