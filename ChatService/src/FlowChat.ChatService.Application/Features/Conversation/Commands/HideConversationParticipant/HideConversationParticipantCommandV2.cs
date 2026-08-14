using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.HideConversationParticipant;

public sealed record HideConversationParticipantCommandV2(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;
