using AutoMapper;
using FlowChat.GatewayService.Api.Models;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.GatewayService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/aggregate/conversations")]
public sealed class ConversationAggregateController : ApiControllerBase
{
    private const int DefaultMessageLimit = 10;

    private readonly IChatServiceClient _chatClient;
    private readonly IMapper _mapper;
    private readonly ILogger<ConversationAggregateController> _logger;

    public ConversationAggregateController(
        IChatServiceClient chatClient,
        IMapper mapper,
        ILogger<ConversationAggregateController> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPut("duet/open")]
    [ProducesResponseType(typeof(OpenDuetConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenDuetConversation(
        [FromBody] OpenDuetConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
        {
            return Unauthorized();
        }

        if (request.PartnerUserId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid partner user id.",
                Detail = "PartnerUserId is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var conversationTask = GetOrCreateConversationAsync(request.PartnerUserId, cancellationToken);
        var knownMessagesTask = request.KnownConversationId.HasValue
            ? GetKnownConversationMessagesOrDefaultAsync(
                request.KnownConversationId.Value,
                request.PartnerUserId,
                cancellationToken)
            : Task.FromResult<ChatMessagesClientDto?>(null);

        var conversation = await conversationTask;
        var messages = await ResolveMessagesAsync(
            conversation,
            request.KnownConversationId,
            knownMessagesTask,
            request.PartnerUserId,
            cancellationToken);

        var response = new OpenDuetConversationResponse(
            conversation.ConversationId,
            _mapper.Map<IReadOnlyCollection<ConversationParticipantResponse>>(conversation.Participants),
            _mapper.Map<IReadOnlyCollection<ConversationMessageResponse>>(messages.Items),
            messages.NextBeforeSentAtUtc,
            messages.NextBeforeMessageId,
            messages.HasMore);

        return Ok(response);
    }

    [HttpPut("group/open")]
    [ProducesResponseType(typeof(OpenGroupConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenGroupConversation(
        [FromBody] OpenGroupConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out _))
        {
            return Unauthorized();
        }

        if (request.ConversationId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid conversation id.",
                Detail = "ConversationId is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var conversation = await _chatClient.GetGroupConversationAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Group conversation not found.",
                Detail = "The requested group conversation does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var messages = await _chatClient.GetConversationMessagesAsync(
            request.ConversationId,
            DefaultMessageLimit,
            cancellationToken);

        var response = new OpenGroupConversationResponse(
            conversation.ConversationId,
            conversation.Name,
            _mapper.Map<IReadOnlyCollection<ConversationParticipantResponse>>(conversation.Participants),
            _mapper.Map<IReadOnlyCollection<ConversationMessageResponse>>(messages.Items),
            messages.NextBeforeSentAtUtc,
            messages.NextBeforeMessageId,
            messages.HasMore);

        return Ok(response);
    }
    //ToDo: Dodac na serwisie po postu upsert
    private async Task<DuetConversationClientDto> GetOrCreateConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken) =>
        await _chatClient.GetDuetConversationAsync(partnerUserId, cancellationToken)
            ?? await _chatClient.CreateDuetConversationAsync(partnerUserId, cancellationToken);

    private async Task<ChatMessagesClientDto> ResolveMessagesAsync(
        DuetConversationClientDto conversation,
        Guid? knownConversationId,
        Task<ChatMessagesClientDto?> knownMessagesTask,
        Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        if (knownConversationId.HasValue)
        {
            var knownMessages = await knownMessagesTask;
            if (knownConversationId.Value == conversation.ConversationId && knownMessages is not null)
            {
                return knownMessages;
            }

            if (knownConversationId.Value != conversation.ConversationId)
            {
                _logger.LogInformation(
                    "Client known conversation id {KnownConversationId} differed from Chat Service conversation id {ConversationId} for partner {PartnerUserId}.",
                    knownConversationId.Value,
                    conversation.ConversationId,
                    partnerUserId);
            }
        }

        return await _chatClient.GetConversationMessagesAsync(
            conversation.ConversationId,
            DefaultMessageLimit,
            cancellationToken);
    }

    private async Task<ChatMessagesClientDto?> GetKnownConversationMessagesOrDefaultAsync(
        Guid knownConversationId,
        Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _chatClient.GetConversationMessagesAsync(
                knownConversationId,
                DefaultMessageLimit,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogInformation(
                exception,
                "Failed to prefetch messages for client known conversation id {KnownConversationId} and partner {PartnerUserId}. Falling back to Chat Service conversation lookup.",
                knownConversationId,
                partnerUserId);
            return null;
        }
    }
}
