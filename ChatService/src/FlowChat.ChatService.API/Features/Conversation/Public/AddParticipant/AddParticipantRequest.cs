using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.AddParticipant;

public sealed class AddParticipantRequest : IServiceInput
{
    public IReadOnlyList<Guid> ParticipantUserIds { get; init; } = [];
}
