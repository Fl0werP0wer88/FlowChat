using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationId;

public sealed class GetDuetConversationIdQueryHandler : IQueryHandler<GetDuetConversationIdQuery, Guid>
{
    private readonly IDuetConversationRepository _duetConversationRepository;

    public GetDuetConversationIdQueryHandler(IDuetConversationRepository duetConversationRepository)
    {
        _duetConversationRepository = duetConversationRepository ?? throw new ArgumentNullException(nameof(duetConversationRepository));
    }

    public async Task<FlowChatResult<Guid>> Handle(GetDuetConversationIdQuery request, CancellationToken cancellationToken)
    {
        var conversationId = await _duetConversationRepository.FindConversationIdAsync(
            request.RequestingUserId,
            request.PartnerUserId,
            cancellationToken);

        return conversationId.HasValue
            ? FlowChatResult<Guid>.Success(conversationId.Value)
            : FlowChatResult<Guid>.Failure(DomainError.NotFound("No duet conversation found between the specified users."));
    }
}
