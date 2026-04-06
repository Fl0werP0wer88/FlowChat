using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.InsertUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/insert")]
public sealed class InsertUserProfileProjectionController(
    IUserProfileProjectionRepository userProfileProjectionRepository,
    IUnitOfWork unitOfWork,
    IApiSettingsManager apiSettingsManager)
    : InternalUserProfileProjectionControllerBase(unitOfWork, apiSettingsManager)
{
    private readonly IUserProfileProjectionRepository _userProfileProjectionRepository = userProfileProjectionRepository
        ?? throw new ArgumentNullException(nameof(userProfileProjectionRepository));

    [HttpPost]
    public async Task<IActionResult> Insert(
        [FromBody] UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = ValidateAndMapRequest(request, out var projection);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var wasInserted = await _userProfileProjectionRepository.InsertAsync(projection!, cancellationToken);
        if (!wasInserted)
        {
            return ConflictResponse("User profile projection already exists.");
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return Accepted();
    }
}
