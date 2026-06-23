namespace FlowChat.ChatService.Application.Features.UserProfile;

public sealed record UserProfileConversationParticipantDto(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl);
