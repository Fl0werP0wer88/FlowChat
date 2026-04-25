using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationIds;

public sealed class GetDuetConversationIdsQueryHandler
    : IQueryHandler<GetDuetConversationIdsQuery, IReadOnlyDictionary<Guid, Guid>>
{
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;

    public GetDuetConversationIdsQueryHandler(IDuetConversationReadRepository duetConversationReadRepository)
    {
        _duetConversationReadRepository = duetConversationReadRepository
            ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
    }

    public async Task<FlowChatResult<IReadOnlyDictionary<Guid, Guid>>> Handle(
        GetDuetConversationIdsQuery request,
        CancellationToken cancellationToken)
    {
        var conversationIds = await _duetConversationReadRepository.FindConversationIdsByPartnerIdsAsync(
            request.RequestingUserId,
            request.PartnerUserIds,
            cancellationToken);

        return FlowChatResult<IReadOnlyDictionary<Guid, Guid>>.Success(conversationIds);
    }
}
