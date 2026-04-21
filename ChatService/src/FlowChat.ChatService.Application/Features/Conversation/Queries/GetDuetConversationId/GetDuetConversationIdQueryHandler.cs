using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationId;

public sealed class GetDuetConversationIdQueryHandler : IQueryHandler<GetDuetConversationIdQuery, Guid>
{
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;

    public GetDuetConversationIdQueryHandler(IDuetConversationReadRepository duetConversationReadRepository)
    {
        _duetConversationReadRepository = duetConversationReadRepository ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
    }

    public async Task<FlowChatResult<Guid>> Handle(GetDuetConversationIdQuery request, CancellationToken cancellationToken)
    {
        var conversationId = await _duetConversationReadRepository.FindConversationIdAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            cancellationToken);

        return conversationId.HasValue
            ? FlowChatResult<Guid>.Success(conversationId.Value)
            : FlowChatResult<Guid>.Failure(DomainError.NotFound("No duet conversation found between the specified users."));
    }
}
