using FlowChat.PresenceService.Infrastructure.Configuration;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal;

public abstract class InternalContactObserverProjectionControllerBase(IApiSettingsManager apiSettingsManager) : ApiControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

    protected bool HasValidInternalApiKey()
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
