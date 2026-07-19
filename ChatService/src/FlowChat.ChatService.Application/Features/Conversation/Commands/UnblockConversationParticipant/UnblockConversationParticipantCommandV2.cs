using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.UnblockConversationParticipant;

public sealed record UnblockConversationParticipantCommandV2(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;
