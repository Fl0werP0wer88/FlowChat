namespace FlowChat.UserProfileService.Application.Contracts.Infrastructure;

public interface IEmailVerificationLinkBuilder
{
    string BuildEmailVerificationLink(string token);
}
