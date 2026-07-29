using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
//Review9-2: Wydaje mi sie jednak że było by czytelniej jak tryby before i after były w osobnych query handlerach co ty na to ?
public sealed class GetConversationMessagesQueryHandler
    : IQueryHandler<GetConversationMessagesQuery, ConversationMessagesPageDto>
{
    private readonly IChatMessageReadRepository _chatMessageReadRepository;
    private readonly IConversationMessageSequenceReadRepository _sequenceReadRepository;
    private readonly IConversationParticipantReadRepository _participantReadRepository;

    public GetConversationMessagesQueryHandler(
        IChatMessageReadRepository chatMessageReadRepository,
        IConversationMessageSequenceReadRepository sequenceReadRepository,
        IConversationParticipantReadRepository participantReadRepository)
    {
        _chatMessageReadRepository = chatMessageReadRepository ?? throw new ArgumentNullException(nameof(chatMessageReadRepository));
        _sequenceReadRepository = sequenceReadRepository ?? throw new ArgumentNullException(nameof(sequenceReadRepository));
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

        var currentSequenceNum = await _sequenceReadRepository.GetCurrentAsync(
            request.ConversationId,
            cancellationToken) ?? 0;

        ConversationMessagesPageDto page;
        if (request.AfterSequenceNum.HasValue)
        {
            var throughSequenceNum = request.ThroughSequenceNum ?? currentSequenceNum;
            if (throughSequenceNum < request.AfterSequenceNum.Value)
            {
                return FlowChatResult<ConversationMessagesPageDto>.Failure(
                    DomainError.BadRequest(
                        "ThroughSequenceNum cannot be lower than AfterSequenceNum."));
            }

            if (throughSequenceNum > currentSequenceNum)
            {
                return FlowChatResult<ConversationMessagesPageDto>.Failure(
                    DomainError.BadRequest(
                        "ThroughSequenceNum cannot exceed the current conversation sequence."));
            }

            var rows = await _chatMessageReadRepository.GetAfterSequenceAsync(
                request.ConversationId,
                request.AfterSequenceNum.Value,
                throughSequenceNum,
                request.Limit + 1,
                cancellationToken);
            var hasMore = rows.Count > request.Limit;
            var items = rows.Take(request.Limit).ToList();

            page = new ConversationMessagesPageDto(
                items,
                null,
                hasMore ? items[^1].SequenceNum : null,
                currentSequenceNum,
                throughSequenceNum,
                hasMore);
        }
        else
        {
            var rows = await _chatMessageReadRepository.GetBeforeSequenceAsync(
                request.ConversationId,
                currentSequenceNum,
                request.BeforeSequenceNum,
                request.Limit + 1,
                cancellationToken);
            var hasMore = rows.Count > request.Limit;
            var items = rows.Take(request.Limit).ToList();

            page = new ConversationMessagesPageDto(
                items,
                hasMore ? items[^1].SequenceNum : null,
                null,
                currentSequenceNum,
                null,
                hasMore);
        }

        return FlowChatResult<ConversationMessagesPageDto>.Success(page);
    }
}
