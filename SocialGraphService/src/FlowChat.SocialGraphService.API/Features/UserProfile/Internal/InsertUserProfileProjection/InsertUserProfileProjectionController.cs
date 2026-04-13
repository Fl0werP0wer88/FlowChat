using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.InsertUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/insert")]
public sealed class InsertUserProfileProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public InsertUserProfileProjectionController(IMediator mediator, IApiSettingsManager apiSettingsManager)
        : base(() => apiSettingsManager.GetInternalApiSettings().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(apiSettingsManager);
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
                request.FirstName,
                request.LastName,
                request.Organization,
                request.MainEmailAddress,
                request.MainEmailIsConfirmed,
                request.MainEmailIsVisible,
                request.MainPhoneNumber,
                request.MainPhoneIsConfirmed,
                request.MainPhoneIsVisible,
                request.AvatarUrl,
                request.Bio,
                request.IsActive,
                request.LastSeenAtUtc),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
