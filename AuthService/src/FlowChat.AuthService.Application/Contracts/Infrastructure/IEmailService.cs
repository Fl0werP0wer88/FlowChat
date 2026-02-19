namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isBodyHtml = false,
        CancellationToken cancellationToken = default);
}
