using FlowChat.ChatService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.UpdateUserProfileProjection;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/userprofiles/projection/update")]
public sealed class UpdateUserProfileProjectionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UpdateUserProfileProjectionController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UserProfileProjectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new UpdateUserProfileProjectionCommand(
                request.UserProfileId,
                request.FriendlyUserId,
                request.DisplayName,
                request.AvatarUrl,
                request.CreatedBy,
                request.CreatedAtUtc,
                request.LastModifiedBy,
                request.LastModifiedAtUtc),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
