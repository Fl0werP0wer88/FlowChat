using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;

namespace FlowChat.UserProfileService.Application.Contracts.Infrastructure;

public interface IEmailVerificationTokenProtector
{
    string Protect(EmailVerificationTokenPayload payload);
    bool TryUnprotect(string token, out EmailVerificationTokenPayload? payload);
}
