using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishDuetConversationCreated;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishDuetConversationCreated;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishDuetConversationCreatedController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishDuetConversationCreatedController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("duet-conversations/created/direct")]
    public async Task<IActionResult> Publish([FromBody] PublishDuetConversationCreatedRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishDuetConversationCreatedCommand(
                request.ConversationId,
                request.ParticipantUserIds),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
