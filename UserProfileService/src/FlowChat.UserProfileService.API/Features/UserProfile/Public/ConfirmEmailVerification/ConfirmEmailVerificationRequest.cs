namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationRequest
{
    public string Token { get; set; } = string.Empty;
}
