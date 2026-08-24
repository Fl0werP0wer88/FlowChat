using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsAdded;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishConversationParticipantsAdded;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishConversationParticipantsAddedController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishConversationParticipantsAddedController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("conversations/participants-added/direct")]
    public async Task<IActionResult> Publish([FromBody] PublishConversationParticipantsAddedRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishConversationParticipantsAddedCommand(
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
