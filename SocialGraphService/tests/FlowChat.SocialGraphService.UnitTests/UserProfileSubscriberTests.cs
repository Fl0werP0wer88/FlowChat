using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileSubscriberTests
{
    [Fact]
    public async Task HandleAsync_WhenProfileCreatedEventArrives_PostsNormalizedReadModel()
    {
        var apiClient = new FakeSocialGraphInternalApiClient();
        var subscriber = new UserProfileSubscriber(
            apiClient,
            NullLogger<UserProfileSubscriber>.Instance);

        var userProfileId = Guid.NewGuid();
        await subscriber.HandleAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserProfileId = userProfileId,
                UserName = " john.doe ",
                DisplayName = " John Doe ",
                MainEmail = " john@flowchat.local ",
                MainPhone = " +48123123123 ",
                AvatarUrl = " https://cdn.example/avatar.png ",
                Bio = " hello ",
                IsActive = true,
                LastSeenAtUtc = new DateTime(2026, 3, 11, 10, 0, 0, DateTimeKind.Utc),
                IsEmailVisible = true,
                IsPhoneVisible = false
            },
            CancellationToken.None);

        Assert.NotNull(apiClient.LastRequest);
        Assert.Equal(userProfileId, apiClient.LastRequest!.UserProfileId);
        Assert.Equal("john.doe", apiClient.LastRequest.UserName);
        Assert.Equal("John Doe", apiClient.LastRequest.DisplayName);
        Assert.Equal("john@flowchat.local", apiClient.LastRequest.MainEmail);
        Assert.Equal("+48123123123", apiClient.LastRequest.MainPhone);
        Assert.Equal("https://cdn.example/avatar.png", apiClient.LastRequest.AvatarUrl);
        Assert.Equal("hello", apiClient.LastRequest.Bio);
    }

    [Fact]
    public async Task HandleAsync_WhenStateChangedEventArrives_PostsReadModel()
    {
        var apiClient = new FakeSocialGraphInternalApiClient();
        var subscriber = new UserProfileSubscriber(
            apiClient,
            NullLogger<UserProfileSubscriber>.Instance);

        var userProfileId = Guid.NewGuid();
        await subscriber.HandleAsync(
            new UserProfileStateChangedIntegrationEvent
            {
                UserProfileId = userProfileId,
                UserName = "jane.doe",
                DisplayName = "Jane Doe",
                MainEmail = null,
                MainPhone = "123456",
                AvatarUrl = null,
                Bio = "updated",
                IsActive = false,
                LastSeenAtUtc = null,
                IsEmailVisible = false,
                IsPhoneVisible = true
            },
            CancellationToken.None);

        Assert.NotNull(apiClient.LastRequest);
        Assert.Equal("jane.doe", apiClient.LastRequest!.UserName);
        Assert.Equal("Jane Doe", apiClient.LastRequest.DisplayName);
        Assert.False(apiClient.LastRequest.IsActive);
        Assert.True(apiClient.LastRequest.IsPhoneVisible);
    }

    [Fact]
    public async Task HandleAsync_WhenUserProfileIdIsMissing_ThrowsNonTransientException()
    {
        var apiClient = new FakeSocialGraphInternalApiClient();
        var subscriber = new UserProfileSubscriber(
            apiClient,
            NullLogger<UserProfileSubscriber>.Instance);

        await Assert.ThrowsAsync<NonTransientException>(() => subscriber.HandleAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserProfileId = Guid.Empty,
                UserName = "john.doe",
                DisplayName = "John Doe"
            },
            CancellationToken.None));

        Assert.Null(apiClient.LastRequest);
    }

    private sealed class FakeSocialGraphInternalApiClient : ISocialGraphInternalApiClient
    {
        public UpsertUserProfileReadModelRequest? LastRequest { get; private set; }

        public Task UpsertUserProfileReadModelAsync(
            UpsertUserProfileReadModelRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }
    }
}
