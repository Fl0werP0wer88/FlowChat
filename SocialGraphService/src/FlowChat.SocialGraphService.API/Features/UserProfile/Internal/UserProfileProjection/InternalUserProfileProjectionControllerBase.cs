using FlowChat.Shared.API;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using UserProfileProjectionModel = FlowChat.SocialGraphService.Application.Features.UserProfile.UserProfileProjection;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;

public abstract class InternalUserProfileProjectionControllerBase(
    IUnitOfWork unitOfWork,
    IApiSettingsManager apiSettingsManager) : ApiControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    protected IActionResult? ValidateAndMapRequest(
        UserProfileProjectionRequest request,
        out UserProfileProjectionModel? projection)
    {
        projection = null;

        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        if (request.UserProfileId == Guid.Empty)
        {
            return BadRequestResponse("Payload does not contain valid UserProfileId.");
        }

        if (string.IsNullOrWhiteSpace(request.FriendlyUserId))
        {
            return BadRequestResponse("Payload does not contain valid FriendlyUserId.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return BadRequestResponse("Payload does not contain valid DisplayName.");
        }

        projection = new UserProfileProjectionModel(
            request.UserProfileId,
            request.FriendlyUserId.Trim(),
            request.DisplayName.Trim(),
            NormalizeOptional(request.MainEmail),
            NormalizeOptional(request.MainPhone),
            NormalizeOptional(request.AvatarUrl),
            NormalizeOptional(request.Bio),
            request.IsActive,
            request.LastSeenAtUtc,
            request.IsEmailVisible,
            request.IsPhoneVisible);

        return null;
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
