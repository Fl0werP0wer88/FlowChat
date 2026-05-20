using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversations;

public sealed class GetGroupConversationsQueryHandler
    : IQueryHandler<GetGroupConversationsQuery, IReadOnlyCollection<GroupConversationSummaryDto>>
{
    private readonly IGroupConversationReadRepository _groupConversationReadRepository;

    public GetGroupConversationsQueryHandler(IGroupConversationReadRepository groupConversationReadRepository)
    {
        _groupConversationReadRepository = groupConversationReadRepository
            ?? throw new ArgumentNullException(nameof(groupConversationReadRepository));
    }

    public async Task<FlowChatResult<IReadOnlyCollection<GroupConversationSummaryDto>>> Handle(
        GetGroupConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var conversations = await _groupConversationReadRepository.GetByParticipantUserIdAsync(
            request.ParticipantUserId,
            cancellationToken);

        return FlowChatResult<IReadOnlyCollection<GroupConversationSummaryDto>>.Success(conversations);
    }
}
