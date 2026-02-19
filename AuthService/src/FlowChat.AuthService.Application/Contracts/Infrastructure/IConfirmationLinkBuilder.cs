namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IConfirmationLinkBuilder
{
    string BuildEmailConfirmationLink(Guid userId, string encodedToken);
}
