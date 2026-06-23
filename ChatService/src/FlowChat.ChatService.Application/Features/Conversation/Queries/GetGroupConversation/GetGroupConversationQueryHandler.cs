using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversation;

public sealed class GetGroupConversationQueryHandler : IQueryHandler<GetGroupConversationQuery, GroupConversationDetailDto>
{
    private readonly IGroupConversationReadRepository _groupConversationReadRepository;

    public GetGroupConversationQueryHandler(IGroupConversationReadRepository groupConversationReadRepository)
    {
        _groupConversationReadRepository = groupConversationReadRepository ?? throw new ArgumentNullException(nameof(groupConversationReadRepository));
    }

    public async Task<FlowChatResult<GroupConversationDetailDto>> Handle(
        GetGroupConversationQuery request,
        CancellationToken cancellationToken)
    {
        var conversation = await _groupConversationReadRepository.GetByIdAsync(
            request.ConversationId,
            cancellationToken);

        return conversation is not null
            ? FlowChatResult<GroupConversationDetailDto>.Success(conversation)
            : FlowChatResult<GroupConversationDetailDto>.Failure(
                DomainError.NotFound("Group conversation not found."));
    }
}
