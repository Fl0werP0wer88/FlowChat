using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.AddGroupParticipants;

public sealed class AddGroupParticipantsRequest : IServiceInput
{
    public IReadOnlyList<Guid> ParticipantUserIds { get; init; } = [];
}
