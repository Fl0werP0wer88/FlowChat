using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Queries.GetContactsForUser;

public sealed class GetContactsForUserQueryHandler
    : IQueryHandler<GetContactsForUserQuery, IReadOnlyCollection<ContactDto>>
{
    private readonly IDuetConversationReadRepository _duetConversationReadRepository;

    public GetContactsForUserQueryHandler(IDuetConversationReadRepository duetConversationReadRepository)
    {
        _duetConversationReadRepository = duetConversationReadRepository
            ?? throw new ArgumentNullException(nameof(duetConversationReadRepository));
    }

    public async Task<FlowChatResult<IReadOnlyCollection<ContactDto>>> Handle(
        GetContactsForUserQuery request,
        CancellationToken cancellationToken)
    {
        var contacts = await _duetConversationReadRepository.GetContactsForUserAsync(
            request.RequestingUserId,
            cancellationToken);

        return FlowChatResult<IReadOnlyCollection<ContactDto>>.Success(contacts);
    }
}
