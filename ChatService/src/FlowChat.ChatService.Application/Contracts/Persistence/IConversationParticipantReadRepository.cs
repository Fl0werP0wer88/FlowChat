using FlowChat.ChatService.Application.Features.Conversation.Dtos;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IConversationParticipantReadRepository
{
    Task<IReadOnlyCollection<Guid>?> GetParticipantUserIdsAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ParticipantStateDto>?> GetParticipantStatesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
