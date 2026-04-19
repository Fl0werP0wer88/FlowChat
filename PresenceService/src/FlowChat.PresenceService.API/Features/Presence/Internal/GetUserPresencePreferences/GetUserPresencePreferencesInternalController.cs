using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;
using FlowChat.PresenceService.Application.Features.Presence.Queries.GetUserPresencePreferences;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.PresenceService.API.Features.Presence.Internal.GetUserPresencePreferences;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/presence/preferences")]
public sealed class GetUserPresencePreferencesInternalController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public GetUserPresencePreferencesInternalController(IMediator mediator, ISettingsProvider settingsProvider)
        : base(() => settingsProvider.GetSection<InternalApiSettingsSection>().ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(settingsProvider);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetUserPresencePreferencesQuery(userId),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new UserPresencePreferencesInternalResponse { PreferredStatus = result.Value })
            : HandleError(result.Error);
    }
}

public sealed class UserPresencePreferencesInternalResponse : IServiceOutput
{
    public PresenceStatus? PreferredStatus { get; init; }
}
