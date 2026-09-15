using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsRemoved;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishConversationParticipantsRemoved;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishConversationParticipantsRemovedController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishConversationParticipantsRemovedController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("conversations/participants-removed/direct")]
    public async Task<IActionResult> Publish([FromBody] PublishConversationParticipantsRemovedRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishConversationParticipantsRemovedCommand(
                request.ConversationId,
                request.ConversationType,
                request.ParticipantUserIds,
                request.ParticipantCount,
                request.MembershipRevision),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
