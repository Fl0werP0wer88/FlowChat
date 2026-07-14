using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsAdded;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishGroupConversationParticipantsAdded;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishGroupConversationParticipantsAddedController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishGroupConversationParticipantsAddedController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("group-conversations/participants-added/direct")]
    public async Task<IActionResult> Publish([FromBody] PublishGroupConversationParticipantsAddedRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishGroupConversationParticipantsAddedCommand(
                request.ConversationId,
                request.ParticipantUserIds),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
