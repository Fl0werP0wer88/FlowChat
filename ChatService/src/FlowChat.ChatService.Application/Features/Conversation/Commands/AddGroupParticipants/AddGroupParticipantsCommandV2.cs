using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed record AddGroupParticipantsCommandV2(
    Guid ConversationId,
    IReadOnlyList<Guid> ParticipantUserIds) : ICommand<Unit>;
