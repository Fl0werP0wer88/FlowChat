using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileCreatedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<ISocialGraphInternalApiClient> _apiClientMock = new();
    private readonly UserProfileCreatedSubscriber _subscriber;

    public UserProfileCreatedSubscriberTests()
    {
        _subscriber = new UserProfileCreatedSubscriber(
            _apiClientMock.Object,
            NullLogger<UserProfileCreatedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileCreatedEventArrives_PostsNormalizedReadModel()
    {
        UpsertUserProfileReadModelRequest? capturedRequest = null;
        var userProfileId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.UpsertUserProfileReadModelAsync(It.IsAny<UpsertUserProfileReadModelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UpsertUserProfileReadModelRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
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
                LastSeenAtUtc = new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero),
                IsEmailVisible = true,
                IsPhoneVisible = false
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserProfileId.Should().Be(userProfileId);
        capturedRequest.UserName.Should().Be("john.doe");
        capturedRequest.DisplayName.Should().Be("John Doe");
        capturedRequest.MainEmail.Should().Be("john@flowchat.local");
        capturedRequest.MainPhone.Should().Be("+48123123123");
        capturedRequest.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        capturedRequest.Bio.Should().Be("hello");
    }

    [Fact]
    public async Task HandleAsync_WhenUserProfileIdIsMissing_ThrowsNonTransientException()
    {
        var act = () => _subscriber.HandleAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserProfileId = Guid.Empty,
                UserName = "john.doe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.UpsertUserProfileReadModelAsync(It.IsAny<UpsertUserProfileReadModelRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

public sealed class UserProfileStateChangedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<ISocialGraphInternalApiClient> _apiClientMock = new();
    private readonly UserProfileStateChangedSubscriber _subscriber;

    public UserProfileStateChangedSubscriberTests()
    {
        _subscriber = new UserProfileStateChangedSubscriber(
            _apiClientMock.Object,
            NullLogger<UserProfileStateChangedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenStateChangedEventArrives_PostsReadModel()
    {
        UpsertUserProfileReadModelRequest? capturedRequest = null;

        _apiClientMock
            .Setup(x => x.UpsertUserProfileReadModelAsync(It.IsAny<UpsertUserProfileReadModelRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UpsertUserProfileReadModelRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new UserProfileStateChangedIntegrationEvent
            {
                UserProfileId = _fixture.Create<Guid>(),
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

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserName.Should().Be("jane.doe");
        capturedRequest.DisplayName.Should().Be("Jane Doe");
        capturedRequest.IsActive.Should().BeFalse();
        capturedRequest.IsPhoneVisible.Should().BeTrue();
    }
}
