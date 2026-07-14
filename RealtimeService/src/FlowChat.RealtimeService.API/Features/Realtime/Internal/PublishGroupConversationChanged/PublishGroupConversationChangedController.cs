using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationChanged;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishGroupConversationChanged;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/realtime")]
public sealed class PublishGroupConversationChangedController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public PublishGroupConversationChangedController(IMediator mediator, IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpPost("group-conversations/changed/direct")]
    public async Task<IActionResult> Publish([FromBody] PublishGroupConversationChangedRequest request, CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new PublishGroupConversationChangedCommand(
                request.ConversationId,
                request.Type,
                request.Name,
                request.CreatedByUserId),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }
}
