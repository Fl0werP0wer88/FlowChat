using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Consumers.Configuration;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using FlowChat.NotificationService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.NotificationService.UnitTests;

public sealed class UserEmailVerificationRequestedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<INotificationInternalApiClient> _apiClientMock = new();
    private readonly UserEmailVerificationRequestedSubscriber _subscriber;

    public UserEmailVerificationRequestedSubscriberTests()
    {
        _subscriber = new UserEmailVerificationRequestedSubscriber(
            _apiClientMock.Object,
            NullLogger<UserEmailVerificationRequestedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenEventArrives_PostsNormalizedRequest()
    {
        ProcessUserEmailVerificationRequestedRequest? capturedRequest = null;
        var userId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.ProcessUserEmailVerificationRequestedAsync(
                It.IsAny<ProcessUserEmailVerificationRequestedRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProcessUserEmailVerificationRequestedRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                Key = "request-123",
                UserId = userId,
                UserEmail = " john.doe@flowchat.local ",
                ConfirmationLink = " https://localhost/confirm "
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(userId);
        capturedRequest.Email.Should().Be("john.doe@flowchat.local");
        capturedRequest.UserName.Should().Be("john.doe");
        capturedRequest.DisplayName.Should().Be("john.doe");
        capturedRequest.ConfirmationLink.Should().Be("https://localhost/confirm");
        capturedRequest.SourceMessageKey.Should().Be("request-123");
    }

    [Fact]
    public async Task HandleAsync_WhenUserEmailIsMissing_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                UserEmail = " ",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.ProcessUserEmailVerificationRequestedAsync(
                It.IsAny<ProcessUserEmailVerificationRequestedRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenConfirmationLinkIsMissing_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                UserEmail = "john.doe@flowchat.local",
                ConfirmationLink = " "
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.ProcessUserEmailVerificationRequestedAsync(
                It.IsAny<ProcessUserEmailVerificationRequestedRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
