using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishGroupConversationParticipantsRemoved;

public sealed class PublishGroupConversationParticipantsRemovedRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}
