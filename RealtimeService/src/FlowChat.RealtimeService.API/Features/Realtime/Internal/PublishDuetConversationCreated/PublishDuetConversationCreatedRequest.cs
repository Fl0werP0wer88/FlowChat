using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishDuetConversationCreated;

public sealed class PublishDuetConversationCreatedRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}
