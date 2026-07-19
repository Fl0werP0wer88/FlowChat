using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.BlockConversationParticipant;

public sealed record BlockConversationParticipantCommandV2(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;
