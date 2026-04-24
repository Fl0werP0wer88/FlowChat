using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandler
    : IQueryHandler<GetConversationMessagesQuery, ConversationMessagesPageDto>
{
    private readonly IChatMessageReadRepository _chatMessageReadRepository;
    private readonly IConversationWriteRepository _conversationRepository;

    public GetConversationMessagesQueryHandler(
        IChatMessageReadRepository chatMessageReadRepository,
        IConversationWriteRepository conversationRepository)
    {
        _chatMessageReadRepository = chatMessageReadRepository ?? throw new ArgumentNullException(nameof(chatMessageReadRepository));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    }

    public async Task<FlowChatResult<ConversationMessagesPageDto>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return FlowChatResult<ConversationMessagesPageDto>.Failure(DomainError.NotFound("Conversation not found."));
        }

        if (!conversation.Participants.Any(participant => participant.UserId == request.RequestingUserId))
        {
            return FlowChatResult<ConversationMessagesPageDto>.Failure(
                DomainError.Unauthorized("Requesting user is not a participant of this conversation."));
        }

        var page = await _chatMessageReadRepository.GetPageBeforeAsync(
            request.ConversationId,
            request.Limit,
            request.BeforeSentAtUtc,
            request.BeforeMessageId,
            cancellationToken);

        return FlowChatResult<ConversationMessagesPageDto>.Success(page);
    }
}
