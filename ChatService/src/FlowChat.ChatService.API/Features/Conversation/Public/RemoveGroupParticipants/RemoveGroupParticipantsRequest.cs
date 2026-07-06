using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsRequest : IServiceInput
{
    public IReadOnlyList<Guid> ParticipantUserIds { get; init; } = [];
}
