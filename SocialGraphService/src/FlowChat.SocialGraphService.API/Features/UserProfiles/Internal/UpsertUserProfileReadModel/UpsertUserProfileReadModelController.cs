using FlowChat.Shared.API;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfiles;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.SocialGraphService.Api.Features.UserProfiles.Internal.UpsertUserProfileReadModel;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles")]
public sealed class UpsertUserProfileReadModelController(
    IUserProfileReadModelRepository userProfileReadModelRepository,
    IUnitOfWork unitOfWork,
    IApiSettingsManager apiSettingsManager) : ApiControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly IUserProfileReadModelRepository _userProfileReadModelRepository = userProfileReadModelRepository
        ?? throw new ArgumentNullException(nameof(userProfileReadModelRepository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

    [HttpPost("read-model")]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertUserProfileReadModelRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        if (request.UserProfileId == Guid.Empty)
        {
            return BadRequestResponse("Payload does not contain valid UserProfileId.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequestResponse("Payload does not contain valid UserName.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequestResponse("Payload does not contain valid DisplayName.");
        }

        await _userProfileReadModelRepository.UpsertAsync(
            new UserProfileReadModel(
                request.UserProfileId,
                request.UserName.Trim(),
                request.DisplayName.Trim(),
                NormalizeOptional(request.MainEmail),
                NormalizeOptional(request.MainPhone),
                NormalizeOptional(request.AvatarUrl),
                NormalizeOptional(request.Bio),
                request.IsActive,
                request.LastSeenAtUtc,
                request.IsEmailVisible,
                request.IsPhoneVisible),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Accepted();
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

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

