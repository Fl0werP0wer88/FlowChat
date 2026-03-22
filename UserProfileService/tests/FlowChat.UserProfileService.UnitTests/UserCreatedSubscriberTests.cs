using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Consumers.Services;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserCreatedSubscriberTests
{
    [Fact]
    public async Task HandleAsync_MapsEmailAndPhoneToInternalApiRequest()
    {
        var userId = Guid.NewGuid();
        var internalApiClient = new CapturingUserProfileInternalApiClient();
        var subscriber = new UserCreatedSubscriber(internalApiClient, NullLogger<UserCreatedSubscriber>.Instance);
        var message = new UserCreatedIntegrationEvent
        {
            UserId = userId,
            UserName = "jdoe",
            DisplayName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "+48123123123"
        };

        await subscriber.HandleAsync(message, CancellationToken.None);

        var request = Assert.IsType<CreateInitialUserProfileRequest>(internalApiClient.LastRequest);
        Assert.Equal("jdoe", request.UserName);
        Assert.Equal("John Doe", request.DisplayName);
        Assert.Equal("john@example.com", request.Email);
        Assert.Equal("+48123123123", request.Phone);
        Assert.Equal(userId, request.UserId);
    }

    private sealed class CapturingUserProfileInternalApiClient : IUserProfileInternalApiClient
    {
        public CreateInitialUserProfileRequest? LastRequest { get; private set; }

        public Task CreateInitialUserProfileAsync(
            CreateInitialUserProfileRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }
    }
}
