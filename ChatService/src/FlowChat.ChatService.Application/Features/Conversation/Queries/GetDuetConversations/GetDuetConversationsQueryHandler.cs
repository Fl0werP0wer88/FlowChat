using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversations;

public sealed class GetDuetConversationsQueryHandler
    : IQueryHandler<GetDuetConversationsQuery, IReadOnlyCollection<DuetConversationListItemDto>>
{
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;

    public GetDuetConversationsQueryHandler(IDuetConversationReadRepository duetConversationReadRepository)
    {
        _duetConversationReadRepository = duetConversationReadRepository
            ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
    }

    public async Task<FlowChatResult<IReadOnlyCollection<DuetConversationListItemDto>>> Handle(
        GetDuetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var conversations = await _duetConversationReadRepository.GetDuetConversationsAsync(
            request.RequestingUserId,
            cancellationToken);

        return FlowChatResult<IReadOnlyCollection<DuetConversationListItemDto>>.Success(conversations);
    }
}
