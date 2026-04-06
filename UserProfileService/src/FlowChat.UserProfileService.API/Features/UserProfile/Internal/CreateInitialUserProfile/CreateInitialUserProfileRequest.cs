namespace FlowChat.UserProfileService.Api.Features.UserProfile.Internal.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileRequest
{
    public string FriendlyUserId { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Organization { get; set; }

    public string? Email { get; set; }

    public Guid UserId { get; set; }
}
