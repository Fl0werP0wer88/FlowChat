namespace FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;

public sealed record EmailVerificationTokenPayload(Guid UserProfileId, Guid EmailId, string Nonce);
