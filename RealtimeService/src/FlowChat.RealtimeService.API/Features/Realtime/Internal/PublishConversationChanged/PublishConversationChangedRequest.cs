using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishConversationChanged;

public sealed class PublishConversationChangedRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
    public int Type { get; init; }
    public string? Name { get; init; }
    public Guid CreatedByUserId { get; init; }
    public IReadOnlyCollection<Guid> ParticipantUserIds { get; init; } = [];
}
