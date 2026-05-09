using AutoFixture;
using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.AuthService.Consumers.Configuration.Settings;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class UserEmailConfirmedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IAuthInternalApiClient> _internalApiClientMock = new();
    private readonly Mock<ILogger<UserEmailConfirmedSubscriber>> _loggerMock = new();
    private readonly UserEmailConfirmedSubscriber _subscriber;

    public UserEmailConfirmedSubscriberTests()
    {
        _subscriber = new UserEmailConfirmedSubscriber(
            _internalApiClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsAuth_MapsEmailToInternalApiRequest()
    {
        AuthEmailConfirmationRequest? capturedRequest = null;
        var emailAddress = "john@example.com";

        _internalApiClientMock
            .Setup(x => x.ConfirmEmailAsync(It.IsAny<AuthEmailConfirmationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AuthEmailConfirmationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = emailAddress,
                IsAuth = true
            }
        };

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.EmailAddress.Should().Be(emailAddress);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsNotAuth_DoesNotCallInternalApi()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = false
            }
        };

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        _internalApiClientMock.Verify(
            x => x.ConfirmEmailAsync(It.IsAny<AuthEmailConfirmationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAddressIsMissing_ThrowsNonTransientException()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "   ",
                IsAuth = true
            }
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*Email.Address*");
    }

    [Fact]
    public async Task HandleAsync_WhenInternalApiThrowsNonTransientException_RethrowsAndLogsInformation()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = true
            }
        };

        _internalApiClientMock
            .Setup(x => x.ConfirmEmailAsync(It.IsAny<AuthEmailConfirmationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonTransientException("boom"));

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");

        VerifyLog(LogLevel.Information, "Skipping UserEmailConfirmedIntegrationEvent in UserEmailConfirmedSubscriber. Reason: boom");
    }

    [Fact]
    public async Task HandleAsync_WhenInternalApiThrowsTransientException_RethrowsAndLogsWarning()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = true
            }
        };

        _internalApiClientMock
            .Setup(x => x.ConfirmEmailAsync(It.IsAny<AuthEmailConfirmationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientException("boom"));

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<TransientException>()
            .WithMessage("boom");

        VerifyLog(LogLevel.Warning, "Transient failure while handling UserEmailConfirmedIntegrationEvent in UserEmailConfirmedSubscriber.");
    }

    private void VerifyLog(LogLevel expectedLogLevel, string expectedMessage)
    {
        _loggerMock.Verify(
            logger => logger.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString() == expectedMessage),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
