using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Interfaces;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;
using FlowChat.GatewayService.Api.Features.Conversation.Interfaces;
using FlowChat.GatewayService.Api.Features.Conversation.Public.OpenDuetConversation;
using FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.Shared.Domain;

namespace FlowChat.GatewayService.Api.Features.Conversation.Services;

public sealed class ConversationFacade(
    IChatServiceClient chatClient,
    IConversationMessagesFacade messagesFacade,
    ILogger<ConversationFacade> logger) : IConversationFacade
{
    private const int DefaultMessageLimit = 10;

    public async Task<FlowChatResult<OpenDuetConversationResult>> OpenDuetAsync(
        Guid requestingUserId,
        Guid partnerUserId,
        Guid? knownConversationId,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUserId(requestingUserId);
        if (validationError is not null)
        {
            return FlowChatResult<OpenDuetConversationResult>.Failure(validationError);
        }

        if (partnerUserId == Guid.Empty)
        {
            return FlowChatResult<OpenDuetConversationResult>.Failure(
                DomainError.BadRequest("PartnerUserId is required."));
        }

        var conversationTask = GetOrCreateDuetConversationAsync(
            partnerUserId,
            cancellationToken);
        var knownMessagesTask = knownConversationId.HasValue
            ? GetKnownConversationMessagesOrDefaultAsync(
                knownConversationId.Value,
                requestingUserId,
                partnerUserId,
                cancellationToken)
            : Task.FromResult<GetConversationMessagesResult?>(null);

        var conversation = await conversationTask;
        var messages = await ResolveMessagesAsync(
            conversation,
            requestingUserId,
            knownConversationId,
            knownMessagesTask,
            partnerUserId,
            cancellationToken);
        if (!messages.IsSuccess)
        {
            return FlowChatResult<OpenDuetConversationResult>.Failure(messages.Error);
        }

        return FlowChatResult<OpenDuetConversationResult>.Success(
            new OpenDuetConversationResult(
                conversation.ConversationId,
                conversation.Participants,
                messages.Value.Items,
                messages.Value.NextBeforeSequenceNum,
                messages.Value.CurrentSequenceNum,
                messages.Value.HasMore));
    }

    public async Task<FlowChatResult<OpenGroupConversationResult>> OpenGroupAsync(
        Guid requestingUserId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUserId(requestingUserId);
        if (validationError is not null)
        {
            return FlowChatResult<OpenGroupConversationResult>.Failure(validationError);
        }

        if (conversationId == Guid.Empty)
        {
            return FlowChatResult<OpenGroupConversationResult>.Failure(
                DomainError.BadRequest("ConversationId is required."));
        }

        var conversation = await chatClient.GetGroupConversationAsync(
            conversationId,
            cancellationToken);
        if (conversation is null)
        {
            return FlowChatResult<OpenGroupConversationResult>.Failure(
                DomainError.NotFound("Group conversation not found."));
        }

        var messages = await GetConversationMessagesAsync(
            conversationId,
            requestingUserId,
            cancellationToken);
        if (!messages.IsSuccess)
        {
            return FlowChatResult<OpenGroupConversationResult>.Failure(messages.Error);
        }

        return FlowChatResult<OpenGroupConversationResult>.Success(
            new OpenGroupConversationResult(
                conversation.ConversationId,
                conversation.Name,
                conversation.Participants,
                messages.Value.Items,
                messages.Value.NextBeforeSequenceNum,
                messages.Value.CurrentSequenceNum,
                messages.Value.HasMore));
    }

    private async Task<DuetConversationClientDto> GetOrCreateDuetConversationAsync(
        Guid partnerUserId,
        CancellationToken cancellationToken) =>
        await chatClient.GetDuetConversationAsync(partnerUserId, cancellationToken)
            ?? await chatClient.CreateDuetConversationAsync(partnerUserId, cancellationToken);

    private async Task<FlowChatResult<GetConversationMessagesResult>> ResolveMessagesAsync(
        DuetConversationClientDto conversation,
        Guid requestingUserId,
        Guid? knownConversationId,
        Task<GetConversationMessagesResult?> knownMessagesTask,
        Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        if (knownConversationId.HasValue)
        {
            var knownMessages = await knownMessagesTask;
            if (knownConversationId.Value == conversation.ConversationId && knownMessages is not null)
            {
                return FlowChatResult<GetConversationMessagesResult>.Success(knownMessages);
            }

            if (knownConversationId.Value != conversation.ConversationId)
            {
                logger.LogInformation(
                    "Client known conversation id {KnownConversationId} differed from Chat Service conversation id {ConversationId} for partner {PartnerUserId}.",
                    knownConversationId.Value,
                    conversation.ConversationId,
                    partnerUserId);
            }
        }

        return await GetConversationMessagesAsync(
            conversation.ConversationId,
            requestingUserId,
            cancellationToken);
    }

    private async Task<GetConversationMessagesResult?> GetKnownConversationMessagesOrDefaultAsync(
        Guid knownConversationId,
        Guid requestingUserId,
        Guid partnerUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await GetConversationMessagesAsync(
                knownConversationId,
                requestingUserId,
                cancellationToken);
            return result.IsSuccess ? result.Value : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogInformation(
                exception,
                "Failed to prefetch messages for client known conversation id {KnownConversationId} and partner {PartnerUserId}. Falling back to Chat Service conversation lookup.",
                knownConversationId,
                partnerUserId);
            return null;
        }
    }

    private Task<FlowChatResult<GetConversationMessagesResult>> GetConversationMessagesAsync(
        Guid conversationId,
        Guid requestingUserId,
        CancellationToken cancellationToken) =>
        messagesFacade.GetHistoryAsync(
            conversationId,
            requestingUserId,
            DefaultMessageLimit,
            null,
            cancellationToken);

    private static DomainError? ValidateUserId(Guid requestingUserId) =>
        requestingUserId == Guid.Empty
            ? DomainError.Unauthorized("Authenticated user id is required.")
            : null;
}
