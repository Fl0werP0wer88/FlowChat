using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.UnmuteConversationParticipant;

public sealed record UnmuteConversationParticipantCommandV2(
    Guid ConversationId,
    Guid RequestingUserId) : ICommand<Unit>;
