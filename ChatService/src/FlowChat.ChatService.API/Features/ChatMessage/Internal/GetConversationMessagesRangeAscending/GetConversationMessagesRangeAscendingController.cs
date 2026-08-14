using AutoMapper;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeAscending;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.API;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Internal.GetConversationMessagesRangeAscending;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/chat/conversations/{conversationId:guid}/messages/range/ascending")]
public sealed class GetConversationMessagesRangeAscendingController : ApiControllerBase
{
    private const int DefaultLimit = 100;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetConversationMessagesRangeAscendingController(
        IMediator mediator,
        IMapper mapper,
        IOptions<InternalApiSettingsSection> internalApiSettings)
        : base(() => internalApiSettings.Value.ApiKey)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        ArgumentNullException.ThrowIfNull(internalApiSettings);
    }

    [HttpGet]
    public async Task<IActionResult> GetRange(
        [FromRoute] Guid conversationId,
        [FromQuery] Guid requestingUserId,
        [FromQuery] long? startSequenceNum = null,
        [FromQuery] long? endSequenceNum = null,
        [FromQuery] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetConversationMessagesRangeAscendingQuery(
                conversationId,
                requestingUserId,
                startSequenceNum,
                endSequenceNum,
                limit),
            cancellationToken);

        return result.IsSuccess
            ? Ok(_mapper.Map<GetConversationMessagesRangeAscendingResponse>(result.Value))
            : HandleError(result.Error);
    }
}
