using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishGroupConversationChanged;

public sealed class PublishGroupConversationChangedRequest : IServiceInput
{
    public Guid ConversationId { get; init; }
    public int Type { get; init; }
    public string? Name { get; init; }
    public Guid CreatedByUserId { get; init; }
}
