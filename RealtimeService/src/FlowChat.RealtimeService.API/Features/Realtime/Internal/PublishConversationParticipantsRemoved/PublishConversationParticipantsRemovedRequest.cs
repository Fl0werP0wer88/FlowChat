using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishConversationParticipantsRemoved;

public sealed class PublishConversationParticipantsRemovedRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
    public int ConversationType { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
    public int ParticipantCount { get; init; }
    public int MembershipRevision { get; init; }
}
