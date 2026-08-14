using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed record CreateGroupConversationCommandV2(
    Guid ConversationId,
    Guid CreatedByUserId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    string Name) : ICommand<GroupConversationDetailDto>;
