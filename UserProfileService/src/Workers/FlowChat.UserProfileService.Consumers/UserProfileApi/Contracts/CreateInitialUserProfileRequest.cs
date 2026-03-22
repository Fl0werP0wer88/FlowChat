namespace FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;

public sealed class CreateInitialUserProfileRequest
{
    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public Guid UserId { get; set; }
}
