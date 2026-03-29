namespace FlowChat.UserProfileService.Application.Features.UserProfiles.EmailVerification;

public sealed record EmailVerificationTokenPayload(Guid UserProfileId, Guid EmailId, string Nonce);
