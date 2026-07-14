using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishGroupConversationParticipantsAdded;

public sealed class PublishGroupConversationParticipantsAddedRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}
