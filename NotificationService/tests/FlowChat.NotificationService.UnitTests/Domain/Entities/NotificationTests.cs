using FlowChat.NotificationService.Domain.Entities.Notification;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace FlowChat.NotificationService.UnitTests.Domain.Entities;

public sealed class NotificationTests
{
    // --- CreateEmailVerification ---

    [Fact]
    public void CreateEmailVerification_WithValidArguments_ReturnsNotificationWithPendingStatus()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.CreateEmailVerification(
            userId,
            EmailAddress.Create("user@example.com"),
            "John Doe",
            "Verify your email here",
            "key-1");

        notification.UserId.Should().Be(userId);
        notification.Email.Value.Should().Be("user@example.com");
        notification.DisplayName.Should().Be("John Doe");
        notification.Body.Should().Be("Verify your email here");
        notification.Type.Should().Be(NotificationType.EmailVerification);
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.SourceMessageKey.Should().Be("key-1");
        notification.ProviderMessageId.Should().BeNull();
        notification.FailureReason.Should().BeNull();
        notification.SentAtUtc.Should().BeNull();
    }

    [Fact]
    public void CreateEmailVerification_WithNullSourceMessageKey_SetsSourceMessageKeyToNull()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John Doe",
            "Verify your email here",
            null);

        notification.SourceMessageKey.Should().BeNull();
    }

    [Fact]
    public void CreateEmailVerification_WithWhitespaceSourceMessageKey_SetsSourceMessageKeyToNull()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John Doe",
            "Verify your email here",
            "   ");

        notification.SourceMessageKey.Should().BeNull();
    }

    [Fact]
    public void CreateEmailVerification_TrimsEmailDisplayNameAndBody()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("  user@example.com  "),
            "  John Doe  ",
            "  Verify your email here  ",
            "key-1");

        notification.Email.Value.Should().Be("user@example.com");
        notification.DisplayName.Should().Be("John Doe");
        notification.Body.Should().Be("Verify your email here");
    }

    [Fact]
    public void CreateEmailVerification_WithEmptyUserId_ThrowsInvalidOperationException()
    {
        var act = () => Notification.CreateEmailVerification(
            Guid.Empty,
            EmailAddress.Create("user@example.com"),
            "John Doe",
            "Verify your email here",
            null);

        act.Should().Throw<InvalidOperationException>().WithMessage("UserId is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateEmailVerification_WithBlankEmail_ThrowsArgumentException(string email)
    {
        var act = () => EmailAddress.Create(email);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateEmailVerification_WithBlankDisplayName_ThrowsInvalidOperationException(string displayName)
    {
        var act = () => Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            displayName,
            "Verify your email here",
            null);

        act.Should().Throw<InvalidOperationException>().WithMessage("DisplayName is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateEmailVerification_WithBlankBody_ThrowsInvalidOperationException(string body)
    {
        var act = () => Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John Doe",
            body,
            null);

        act.Should().Throw<InvalidOperationException>().WithMessage("Body is required.");
    }

    // --- CreateWelcome ---

    [Fact]
    public void CreateWelcome_WithValidArguments_ReturnsNotificationWithWelcomeType()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.CreateWelcome(
            userId,
            EmailAddress.Create("user@example.com"),
            "Jane Doe",
            "Welcome to FlowChat",
            "welcome-key");

        notification.UserId.Should().Be(userId);
        notification.Body.Should().Be("Welcome to FlowChat");
        notification.Type.Should().Be(NotificationType.Welcome);
        notification.Status.Should().Be(NotificationStatus.Pending);
    }

    // --- MarkSent ---

    [Fact]
    public void MarkSent_WithProviderMessageId_SetsStatusToSentAndStoresId()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John",
            "Verify your email here",
            null);

        notification.MarkSent("msg-123");

        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.ProviderMessageId.Should().Be("msg-123");
        notification.FailureReason.Should().BeNull();
        notification.SentAtUtc.Should().NotBeNull();
        notification.SentAtUtc!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkSent_WithNullOrWhitespaceProviderMessageId_SetsProviderMessageIdToNull(string? providerId)
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John",
            "Verify your email here",
            null);

        notification.MarkSent(providerId);

        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.ProviderMessageId.Should().BeNull();
    }

    [Fact]
    public void MarkSent_TrimsProviderMessageId()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John",
            "Verify your email here",
            null);

        notification.MarkSent("  msg-abc  ");

        notification.ProviderMessageId.Should().Be("msg-abc");
    }

    // --- MarkFailed ---

    [Fact]
    public void MarkFailed_WithReason_SetsStatusToFailedAndStoresReason()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John",
            "Verify your email here",
            null);

        notification.MarkFailed("smtp timeout");

        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.FailureReason.Should().Be("smtp timeout");
        notification.ProviderMessageId.Should().BeNull();
        notification.SentAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_WithNullOrWhitespaceReason_UsesDefaultMessage(string? reason)
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John",
            "Verify your email here",
            null);

        notification.MarkFailed(reason);

        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.FailureReason.Should().Be("Unknown notification error.");
    }

    [Fact]
    public void MarkFailed_TrimsFailureReason()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John",
            "Verify your email here",
            null);

        notification.MarkFailed("  smtp error  ");

        notification.FailureReason.Should().Be("smtp error");
    }

    [Fact]
    public void MarkFailed_AfterMarkSent_ClearsSentAtUtcAndProviderMessageId()
    {
        var notification = Notification.CreateEmailVerification(
            Guid.NewGuid(),
            EmailAddress.Create("user@example.com"),
            "John",
            "Verify your email here",
            null);
        notification.MarkSent("msg-123");

        notification.MarkFailed("retry failed");

        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.ProviderMessageId.Should().BeNull();
        notification.SentAtUtc.Should().BeNull();
    }
}
