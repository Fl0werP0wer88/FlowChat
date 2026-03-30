using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class UserEmailConfirmedSubscriberTests
{
    [Fact]
    public async Task HandleAsync_WhenEmailIsAuth_MapsEmailToInternalApiRequest()
    {
        var internalApiClient = new CapturingAuthInternalApiClient();
        var subscriber = new UserEmailConfirmedSubscriber(internalApiClient, NullLogger<UserEmailConfirmedSubscriber>.Instance);
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = Guid.NewGuid(),
            EmailId = Guid.NewGuid(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = true
            }
        };

        await subscriber.HandleAsync(message, CancellationToken.None);

        var request = Assert.IsType<AuthEmailConfirmationRequest>(internalApiClient.LastRequest);
        Assert.Equal("john@example.com", request.EmailAddress);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsNotAuth_DoesNotCallInternalApi()
    {
        var internalApiClient = new CapturingAuthInternalApiClient();
        var subscriber = new UserEmailConfirmedSubscriber(internalApiClient, NullLogger<UserEmailConfirmedSubscriber>.Instance);
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = Guid.NewGuid(),
            EmailId = Guid.NewGuid(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = false
            }
        };

        await subscriber.HandleAsync(message, CancellationToken.None);

        Assert.Null(internalApiClient.LastRequest);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAddressIsMissing_ThrowsNonTransientException()
    {
        var internalApiClient = new CapturingAuthInternalApiClient();
        var subscriber = new UserEmailConfirmedSubscriber(internalApiClient, NullLogger<UserEmailConfirmedSubscriber>.Instance);
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = Guid.NewGuid(),
            EmailId = Guid.NewGuid(),
            Email = new Email
            {
                Address = "   ",
                IsAuth = true
            }
        };

        var exception = await Assert.ThrowsAsync<NonTransientException>(() => subscriber.HandleAsync(message, CancellationToken.None));

        Assert.Contains("Email.Address", exception.Message);
    }

    private sealed class CapturingAuthInternalApiClient : IAuthInternalApiClient
    {
        public AuthEmailConfirmationRequest? LastRequest { get; private set; }

        public Task ConfirmEmailAsync(AuthEmailConfirmationRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }
    }
}
