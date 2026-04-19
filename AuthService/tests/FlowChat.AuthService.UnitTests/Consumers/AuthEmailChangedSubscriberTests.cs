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

public sealed class AuthEmailChangedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IAuthInternalApiClient> _internalApiClientMock = new();
    private readonly Mock<ILogger<AuthEmailChangedSubscriber>> _loggerMock = new();
    private readonly AuthEmailChangedSubscriber _subscriber;

    public AuthEmailChangedSubscriberTests()
    {
        _subscriber = new AuthEmailChangedSubscriber(
            _internalApiClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenPayloadIsValid_MapsRequestToInternalApi()
    {
        AuthEmailChangeRequest? capturedRequest = null;
        var userId = _fixture.Create<Guid>();

        _internalApiClientMock
            .Setup(x => x.ChangeAuthEmailAsync(It.IsAny<AuthEmailChangeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AuthEmailChangeRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        var message = new AuthEmailChangedIntegrationEvent
        {
            UserProfileId = userId,
            EmailId = _fixture.Create<Guid>(),
            EmailAddress = "john@example.com"
        };

        await _subscriber.HandleAsync(message, CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(userId);
        capturedRequest.EmailAddress.Should().Be("john@example.com");
    }

    [Fact]
    public async Task HandleAsync_WhenUserProfileIdIsMissing_ThrowsNonTransientException()
    {
        var message = new AuthEmailChangedIntegrationEvent
        {
            UserProfileId = Guid.Empty,
            EmailId = _fixture.Create<Guid>(),
            EmailAddress = "john@example.com"
        };

        var act = () => _subscriber.HandleAsync(message, CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*UserProfileId*");
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAddressIsMissing_ThrowsNonTransientException()
    {
        var message = new AuthEmailChangedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            EmailAddress = "   "
        };

        var act = () => _subscriber.HandleAsync(message, CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*EmailAddress*");
    }
}
