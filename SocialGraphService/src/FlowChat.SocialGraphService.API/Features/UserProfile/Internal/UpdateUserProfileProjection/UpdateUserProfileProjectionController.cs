using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UpdateUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/update")]
public sealed class UpdateUserProfileProjectionController(
    IUserProfileProjectionRepository userProfileProjectionRepository,
    IUnitOfWork unitOfWork,
    IApiSettingsManager apiSettingsManager)
    : InternalUserProfileProjectionControllerBase(unitOfWork, apiSettingsManager)
{
    private readonly IUserProfileProjectionRepository _userProfileProjectionRepository = userProfileProjectionRepository
        ?? throw new ArgumentNullException(nameof(userProfileProjectionRepository));

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateAndMapRequest(request, out var projection);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var wasUpdated = await _userProfileProjectionRepository.UpdateAsync(projection!, cancellationToken);
        if (!wasUpdated)
        {
            return NotFoundResponse("User profile projection was not found.");
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return Accepted();
    }
}
