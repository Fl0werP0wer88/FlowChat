using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IDuetConversationReadRepository
{
    Task<Guid?> FindConversationIdAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);

    Task<DuetConversationDetailDto?> GetByUserIdsAsync(
        Guid requestingUserId,
        Guid partnerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContactDto>> GetContactsForUserAsync(
        Guid requestingUserId,
        CancellationToken cancellationToken = default);
}
