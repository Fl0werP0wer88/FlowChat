using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FlowChat.NotificationService.Infrastructure.Services;

public sealed class NotificationSender : INotificationSender
{
    private readonly IApiSettingsManager _apiSettingsManager;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(
        IApiSettingsManager apiSettingsManager,
        ILogger<NotificationSender> logger)
    {
        _apiSettingsManager = apiSettingsManager;
        _logger = logger;
    }

    public async Task<NotificationSendResult> SendAsync(
        NotificationSendRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var emailSettings = _apiSettingsManager.GetEmailSettingsSection();
        var smtpHost = emailSettings.SmtpHost;
        var smtpPort = emailSettings.SmtpPort;
        var fromEmail = emailSettings.FromEmail;
        var fromName = emailSettings.FromName;
        var username = emailSettings.Username;
        var password = emailSettings.Password;

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            throw new InvalidOperationException("Missing configuration value: EmailSettingsSection:SmtpHost.");
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Missing configuration value: EmailSettingsSection:FromEmail.");
        }

        if (smtpPort <= 0)
        {
            throw new InvalidOperationException("Invalid configuration value: EmailSettingsSection:SmtpPort.");
        }

        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            throw new InvalidOperationException("Recipient email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new InvalidOperationException("Email subject is required.");
        }

        var enableSsl = emailSettings.EnableSsl;
        var secureSocketOptions = enableSsl
            ? (smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;

        var message = new MimeMessage();
        message.From.Add(string.IsNullOrWhiteSpace(fromName)
            ? MailboxAddress.Parse(fromEmail)
            : new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(request.RecipientEmail));
        message.Subject = request.Subject;
        message.Body = new BodyBuilder
        {
            TextBody = request.Body
        }.ToMessageBody();

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

            var providerMessageId = string.IsNullOrWhiteSpace(message.MessageId)
                ? $"smtp-{Guid.NewGuid():N}"
                : message.MessageId;

            _logger.LogInformation(
                "Email sent to {RecipientEmail} for user {UserId}. MessageId: {MessageId}",
                request.RecipientEmail,
                request.UserId,
                providerMessageId);

            return new NotificationSendResult(
                IsSuccess: true,
                ProviderMessageId: providerMessageId,
                Error: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {RecipientEmail} for user {UserId}.",
                request.RecipientEmail,
                request.UserId);

            return new NotificationSendResult(
                IsSuccess: false,
                ProviderMessageId: null,
                Error: ex.Message);
        }
    }
}
