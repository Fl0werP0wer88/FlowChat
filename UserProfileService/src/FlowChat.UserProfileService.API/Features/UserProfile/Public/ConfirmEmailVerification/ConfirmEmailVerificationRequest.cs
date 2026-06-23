using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationRequest : IServiceInput
{
    public string Token { get; set; } = string.Empty;
}
