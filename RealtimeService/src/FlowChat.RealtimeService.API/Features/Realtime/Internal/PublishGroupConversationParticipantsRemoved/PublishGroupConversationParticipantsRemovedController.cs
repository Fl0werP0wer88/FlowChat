using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsRemoved;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishGroupConversationParticipantsRemoved;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishGroupConversationParticipantsRemovedController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishGroupConversationParticipantsRemovedController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("group-conversations/participants-removed/direct")]
    public async Task<IActionResult> Publish([FromBody] PublishGroupConversationParticipantsRemovedRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishGroupConversationParticipantsRemovedCommand(
                request.ConversationId,
                request.ParticipantUserIds),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
