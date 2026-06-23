using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandler
    : IQueryHandler<GetConversationMessagesQuery, ConversationMessagesPageDto>
{
    private readonly IChatMessageReadRepository _chatMessageReadRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;

    public GetConversationMessagesQueryHandler(
        IChatMessageReadRepository chatMessageReadRepository,
        IConversationParticipantReadRepository participantReadRepository)
    {
        _chatMessageReadRepository = chatMessageReadRepository ?? throw new ArgumentNullException(nameof(chatMessageReadRepository));
        _participantReadRepository = participantReadRepository ?? throw new ArgumentNullException(nameof(participantReadRepository));
    }

    public async Task<FlowChatResult<ConversationMessagesPageDto>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = await _participantReadRepository.GetParticipantUserIdsAsync(
            request.ConversationId,
            cancellationToken);
        if (participantUserIds is null)
        {
            return FlowChatResult<ConversationMessagesPageDto>.Failure(DomainError.NotFound("Conversation not found."));
        }

        if (!participantUserIds.Contains(request.RequestingUserId))
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
