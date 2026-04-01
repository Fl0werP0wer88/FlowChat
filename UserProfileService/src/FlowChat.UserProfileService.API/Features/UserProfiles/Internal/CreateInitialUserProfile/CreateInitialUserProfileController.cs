using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.Internal.CreateInitialUserProfile;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles")]
public sealed class CreateInitialUserProfileController(
    IMediator mediator,
    IApiSettingsManager apiSettingsManager) : ApiControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

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
                request.UserName,
                request.DisplayName,
                request.AvatarUrl,
                request.Bio,
                request.Email,
                request.Phone,
                request.UserId),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }

    private bool HasValidInternalApiKey()
    {
        var expectedApiKey = _apiSettingsManager.GetInternalApiSettings().ApiKey;
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return false;
        }

        if (!Request.Headers.TryGetValue(InternalApiKeyHeaderName, out var providedApiKey))
        {
            return false;
        }

        return string.Equals(providedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal);
    }
}

