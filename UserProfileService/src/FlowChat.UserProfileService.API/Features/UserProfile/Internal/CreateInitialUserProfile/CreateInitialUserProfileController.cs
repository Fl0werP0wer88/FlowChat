using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Internal.CreateInitialUserProfile;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles")]
public sealed class CreateInitialUserProfileController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CreateInitialUserProfileController(IMediator mediator, IApiSettingsManager apiSettingsManager)
        : base(() => apiSettingsManager.GetInternalApiSettingsSection().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(apiSettingsManager);
    }

    [HttpPost("initial")]
    public async Task<IActionResult> CreateInitialUserProfile(
        [FromBody] CreateInitialUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new CreateInitialUserProfileCommand(
                request.FriendlyUserId,
                request.Email,
                request.UserId,
                request.FirstName,
                request.LastName,
                request.Organization),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}

