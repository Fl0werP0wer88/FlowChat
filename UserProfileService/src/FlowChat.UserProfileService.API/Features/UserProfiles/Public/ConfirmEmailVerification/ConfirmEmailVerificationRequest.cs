namespace FlowChat.UserProfileService.Api.Features.UserProfiles.Public.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationRequest
{
    public string Token { get; set; } = string.Empty;
}
