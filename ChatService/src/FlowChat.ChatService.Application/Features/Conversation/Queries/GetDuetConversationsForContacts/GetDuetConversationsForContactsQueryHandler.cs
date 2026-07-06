using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationsForContacts;

public sealed class GetDuetConversationsForContactsQueryHandler
    : IQueryHandler<GetDuetConversationsForContactsQuery, IReadOnlyCollection<DuetConversationForContactDto>>
{
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;

    public GetDuetConversationsForContactsQueryHandler(IDuetConversationReadRepository duetConversationReadRepository)
    {
        _duetConversationReadRepository = duetConversationReadRepository
            ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
    }

    public async Task<FlowChatResult<IReadOnlyCollection<DuetConversationForContactDto>>> Handle(
        GetDuetConversationsForContactsQuery request,
        CancellationToken cancellationToken)
    {
        var conversations = await _duetConversationReadRepository.GetConversationsForContactsAsync(
            request.RequestingUserId,
            request.PartnerUserIds,
            cancellationToken);

        return FlowChatResult<IReadOnlyCollection<DuetConversationForContactDto>>.Success(conversations);
    }
}
