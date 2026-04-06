using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Consumers.Services;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AccountRegisteredSubscriberTests
{
    private readonly Mock<IUserProfileInternalApiClient> _apiClientMock = new();

    public AccountRegisteredSubscriberTests()
    {
        _apiClientMock
            .Setup(x => x.CreateInitialUserProfileAsync(It.IsAny<CreateInitialUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_MapsEmailAndPhoneToInternalApiRequest()
    {
        var userId = Guid.NewGuid();
        CreateInitialUserProfileRequest? capturedRequest = null;
        _apiClientMock
            .Setup(x => x.CreateInitialUserProfileAsync(It.IsAny<CreateInitialUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInitialUserProfileRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);

        var subscriber = new AccountRegisteredSubscriber(_apiClientMock.Object, NullLogger<AccountRegisteredSubscriber>.Instance);
        var message = new AccountRegisteredIntegrationEvent
        {
            UserId = userId,
            FriendlyUserId = "jdoe",
            DisplayName = "johnny",
            Email = "john@example.com",
            PhoneNumber = "+48123123123",
            FirstName = " John ",
            LastName = " Doe ",
            Organization = " FlowChat "
        };

        await subscriber.HandleAsync(message, CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Should().BeOfType<CreateInitialUserProfileRequest>();
        capturedRequest.FriendlyUserId.Should().Be("jdoe");
        capturedRequest.DisplayName.Should().Be("John Doe");
        capturedRequest.FirstName.Should().Be("John");
        capturedRequest.LastName.Should().Be("Doe");
        capturedRequest.Organization.Should().Be("FlowChat");
        capturedRequest.Email.Should().Be("john@example.com");
        capturedRequest.Phone.Should().Be("+48123123123");
        capturedRequest.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HandleAsync_WhenFriendlyUserIdIsMissing_ThrowsNonTransientException()
    {
        var subscriber = new AccountRegisteredSubscriber(
            _apiClientMock.Object,
            NullLogger<AccountRegisteredSubscriber>.Instance);

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            subscriber.HandleAsync(
                new AccountRegisteredIntegrationEvent
                {
                    UserId = Guid.NewGuid(),
                    FriendlyUserId = "   ",
                    DisplayName = "John Doe",
                    Email = "test@example.com",
                    PhoneNumber = "+48123123123"
                },
                CancellationToken.None));

        exception.Message.Should().Contain("FriendlyUserId");
    }

    [Fact]
    public async Task HandleAsync_WhenPhoneIsMissing_MapsNullPhone()
    {
        var userId = Guid.NewGuid();
        CreateInitialUserProfileRequest? capturedRequest = null;
        _apiClientMock
            .Setup(x => x.CreateInitialUserProfileAsync(It.IsAny<CreateInitialUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInitialUserProfileRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);

        var subscriber = new AccountRegisteredSubscriber(_apiClientMock.Object, NullLogger<AccountRegisteredSubscriber>.Instance);
        var message = new AccountRegisteredIntegrationEvent
        {
            UserId = userId,
            FriendlyUserId = "jdoe",
            DisplayName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = null
        };

        await subscriber.HandleAsync(message, CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Phone.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenFirstAndLastNameMissing_FallsBackToPayloadDisplayName()
    {
        CreateInitialUserProfileRequest? capturedRequest = null;
        _apiClientMock
            .Setup(x => x.CreateInitialUserProfileAsync(It.IsAny<CreateInitialUserProfileRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateInitialUserProfileRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);

        var subscriber = new AccountRegisteredSubscriber(_apiClientMock.Object, NullLogger<AccountRegisteredSubscriber>.Instance);

        await subscriber.HandleAsync(
            new AccountRegisteredIntegrationEvent
            {
                UserId = Guid.NewGuid(),
                FriendlyUserId = "jdoe",
                DisplayName = " John Doe ",
                Email = "john@example.com"
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.DisplayName.Should().Be("John Doe");
    }
}
