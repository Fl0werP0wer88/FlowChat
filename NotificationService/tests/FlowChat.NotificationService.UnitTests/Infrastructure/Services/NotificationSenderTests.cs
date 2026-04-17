using FlowChat.Core.Contracts;
using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Infrastructure.Configuration;
using FlowChat.NotificationService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowChat.NotificationService.UnitTests.Infrastructure.Services;

public sealed class NotificationSenderTests
{
    private readonly Mock<ISettingsProvider> _settingsProviderMock = new();
    private readonly Mock<ILogger<NotificationSender>> _loggerMock = new();
    private readonly NotificationSender _sender;

    public NotificationSenderTests()
    {
        _sender = new NotificationSender(_settingsProviderMock.Object, _loggerMock.Object);
    }

    private void SetupEmailSettingsSection(
        string smtpHost = "smtp.example.com",
        int smtpPort = 587,
        string fromEmail = "noreply@example.com",
        string fromName = "FlowChat",
        bool enableSsl = false)
    {
        _settingsProviderMock
            .Setup(x => x.GetSection<EmailSettingsSection>())
            .Returns(new EmailSettingsSection
            {
                SmtpHost = smtpHost,
                SmtpPort = smtpPort,
                FromEmail = fromEmail,
                FromName = fromName,
                EnableSsl = enableSsl,
                Username = string.Empty,
                Password = string.Empty
            });
    }

    [Fact]
    public async Task SendAsync_WhenSmtpHostIsMissing_ThrowsInvalidOperationException()
    {
        SetupEmailSettingsSection(smtpHost: "");

        var request = new NotificationSendRequest(Guid.NewGuid(), "user@example.com", "Test Subject", "Body");

        var act = () => _sender.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SmtpHost*");
    }

    [Fact]
    public async Task SendAsync_WhenFromEmailIsMissing_ThrowsInvalidOperationException()
    {
        SetupEmailSettingsSection(fromEmail: "");

        var request = new NotificationSendRequest(Guid.NewGuid(), "user@example.com", "Test Subject", "Body");

        var act = () => _sender.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*FromEmail*");
    }

    [Fact]
    public async Task SendAsync_WhenSmtpPortIsZero_ThrowsInvalidOperationException()
    {
        SetupEmailSettingsSection(smtpPort: 0);

        var request = new NotificationSendRequest(Guid.NewGuid(), "user@example.com", "Test Subject", "Body");

        var act = () => _sender.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SmtpPort*");
    }

    [Fact]
    public async Task SendAsync_WhenRecipientEmailIsMissing_ThrowsInvalidOperationException()
    {
        SetupEmailSettingsSection();

        var request = new NotificationSendRequest(Guid.NewGuid(), "", "Test Subject", "Body");

        var act = () => _sender.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Recipient email*");
    }

    [Fact]
    public async Task SendAsync_WhenSubjectIsMissing_ThrowsInvalidOperationException()
    {
        SetupEmailSettingsSection();

        var request = new NotificationSendRequest(Guid.NewGuid(), "user@example.com", "", "Body");

        var act = () => _sender.SendAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*subject*");
    }

    [Fact]
    public async Task SendAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        SetupEmailSettingsSection();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new NotificationSendRequest(Guid.NewGuid(), "user@example.com", "Test Subject", "Body");

        var act = () => _sender.SendAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendAsync_WhenSmtpConnectionFails_ReturnsFailureResult()
    {
        // Set valid settings but unreachable SMTP host — connection will fail
        SetupEmailSettingsSection(smtpHost: "127.0.0.1", smtpPort: 1, fromEmail: "noreply@example.com");

        var request = new NotificationSendRequest(Guid.NewGuid(), "user@example.com", "Test Subject", "Body");

        var result = await _sender.SendAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ProviderMessageId.Should().BeNull();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
