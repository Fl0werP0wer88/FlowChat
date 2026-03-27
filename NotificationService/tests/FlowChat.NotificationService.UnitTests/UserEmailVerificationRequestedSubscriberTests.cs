using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using FlowChat.NotificationService.Consumers.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.NotificationService.UnitTests;

public sealed class UserEmailVerificationRequestedSubscriberTests
{
    [Fact]
    public async Task HandleAsync_WhenEventArrives_PostsNormalizedRequest()
    {
        var apiClient = new FakeNotificationInternalApiClient();
        var subscriber = new UserEmailVerificationRequestedSubscriber(
            apiClient,
            NullLogger<UserEmailVerificationRequestedSubscriber>.Instance);

        var userId = Guid.NewGuid();
        await subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = userId,
                UserEmail = " john.doe@flowchat.local ",
                ConfirmationLink = " https://localhost/confirm "
            },
            CancellationToken.None);

        Assert.NotNull(apiClient.LastRequest);
        Assert.Equal(userId, apiClient.LastRequest!.UserId);
        Assert.Equal("john.doe@flowchat.local", apiClient.LastRequest.Email);
        Assert.Equal("john.doe", apiClient.LastRequest.UserName);
        Assert.Equal("john.doe", apiClient.LastRequest.DisplayName);
        Assert.Equal("https://localhost/confirm", apiClient.LastRequest.ConfirmationLink);
        Assert.Equal(userId.ToString(), apiClient.LastRequest.SourceMessageKey);
    }

    [Fact]
    public async Task HandleAsync_WhenUserEmailMissing_ThrowsNonTransientException()
    {
        var apiClient = new FakeNotificationInternalApiClient();
        var subscriber = new UserEmailVerificationRequestedSubscriber(
            apiClient,
            NullLogger<UserEmailVerificationRequestedSubscriber>.Instance);

        await Assert.ThrowsAsync<NonTransientException>(() => subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = Guid.NewGuid(),
                UserEmail = " ",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None));

        Assert.Null(apiClient.LastRequest);
    }

    [Fact]
    public async Task HandleAsync_WhenConfirmationLinkMissing_ThrowsNonTransientException()
    {
        var apiClient = new FakeNotificationInternalApiClient();
        var subscriber = new UserEmailVerificationRequestedSubscriber(
            apiClient,
            NullLogger<UserEmailVerificationRequestedSubscriber>.Instance);

        await Assert.ThrowsAsync<NonTransientException>(() => subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = Guid.NewGuid(),
                UserEmail = "john.doe@flowchat.local",
                ConfirmationLink = " "
            },
            CancellationToken.None));

        Assert.Null(apiClient.LastRequest);
    }

    private sealed class FakeNotificationInternalApiClient : INotificationInternalApiClient
    {
        public ProcessUserEmailVerificationRequestedRequest? LastRequest { get; private set; }

        public Task ProcessUserEmailVerificationRequestedAsync(
            ProcessUserEmailVerificationRequestedRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }
    }
}
