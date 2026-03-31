using AutoFixture;
using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class UserEmailConfirmedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IAuthInternalApiClient> _internalApiClientMock = new();
    private readonly UserEmailConfirmedSubscriber _subscriber;

    public UserEmailConfirmedSubscriberTests()
    {
        _subscriber = new UserEmailConfirmedSubscriber(
            _internalApiClientMock.Object,
            NullLogger<UserEmailConfirmedSubscriber>.Instance);
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

        await _subscriber.HandleAsync(message, CancellationToken.None);

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

        await _subscriber.HandleAsync(message, CancellationToken.None);

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

        var act = () => _subscriber.HandleAsync(message, CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*Email.Address*");
    }
}
