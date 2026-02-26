using FlowChat.AuthService.Application.Contracts.Infrastructure;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FlowChat.AuthService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isBodyHtml = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPortValue = _configuration["EmailSettings:SmtpPort"];

        var enableSslValue = _configuration["EmailSettings:EnableSsl"];
        var fromEmail = _configuration["EmailSettings:FromEmail"];
        var fromName = _configuration["EmailSettings:FromName"];
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            throw new InvalidOperationException("Missing configuration value: EmailSettings:SmtpHost.");
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Missing configuration value: EmailSettings:FromEmail.");
        }

        if (string.IsNullOrWhiteSpace(smtpPortValue) || !int.TryParse(smtpPortValue, out var smtpPort))
        {
            throw new InvalidOperationException("Invalid configuration value: EmailSettings:SmtpPort.");
        }

        var enableSsl = bool.TryParse(enableSslValue, out var parsedEnableSsl) && parsedEnableSsl;
        var secureSocketOptions = enableSsl
            ? (smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;

        var message = new MimeMessage();
        message.From.Add(string.IsNullOrWhiteSpace(fromName)
            ? MailboxAddress.Parse(fromEmail)
            : new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isBodyHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();
        smtpClient.CheckCertificateRevocation = false;

        try
        {
            await smtpClient.ConnectAsync(smtpHost, smtpPort, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                await smtpClient.AuthenticateAsync(username, password, cancellationToken);
            }

            await smtpClient.SendAsync(message, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {Recipient}.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}.", to);
            throw;
        }
    }
}
