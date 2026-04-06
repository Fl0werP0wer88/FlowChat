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
    IUserProfileProjectionWriteRepository userProfileProjectionWriteRepository,
    IUnitOfWork unitOfWork,
    IApiSettingsManager apiSettingsManager)
    : InternalUserProfileProjectionControllerBase(unitOfWork, apiSettingsManager)
{
    private readonly IUserProfileProjectionWriteRepository _userProfileProjectionWriteRepository = userProfileProjectionWriteRepository
        ?? throw new ArgumentNullException(nameof(userProfileProjectionWriteRepository));

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

        var wasUpdated = await _userProfileProjectionWriteRepository.UpdateAsync(projection!, cancellationToken);
        if (!wasUpdated)
        {
            return NotFoundResponse("User profile projection was not found.");
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return Accepted();
    }
}
