using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Internal.CreateInitialUserProfile;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles")]
public sealed class CreateInitialUserProfileController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public CreateInitialUserProfileController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
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

        if (!result.IsSuccess)
        {
            return HandleError(result.Error);
        }

        return result.Value.WasAlreadyProcessed
            ? Ok()
            : StatusCode(StatusCodes.Status201Created);
    }
}

