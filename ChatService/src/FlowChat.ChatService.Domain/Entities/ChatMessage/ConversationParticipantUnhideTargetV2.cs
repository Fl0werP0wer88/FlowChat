using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Domain.Entities.ChatMessage;

public sealed record ConversationParticipantUnhideTargetV2(
    Id<ConversationParticipant> ParticipantId,
    Id<UserProfileMarker> UserId);
