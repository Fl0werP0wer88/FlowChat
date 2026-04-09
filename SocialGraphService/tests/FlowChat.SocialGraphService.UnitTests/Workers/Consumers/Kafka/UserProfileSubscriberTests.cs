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
    public async Task HandleAsync_WhenProfileCreatedEventArrives_InsertsNormalizedProjection()
    {
        UserProfileProjectionRequest? capturedRequest = null;
        var userProfileId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.InsertUserProfileProjectionAsync(It.IsAny<UserProfileProjectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new UserProfileCreatedIntegrationEvent
            {
                UserProfileId = userProfileId,
                FriendlyUserId = " john.doe ",
                FirstName = " John ",
                LastName = " Doe ",
                Organization = " FlowChat ",
                MainEmail = new UserProfileEmail
                {
                    Id = _fixture.Create<Guid>(),
                    UserProfileId = userProfileId,
                    Address = " john@flowchat.local ",
                    IsMain = true,
                    IsAuth = true,
                    IsConfirmed = false,
                    IsVisible = true
                },
                MainPhone = new UserProfilePhone
                {
                    Id = _fixture.Create<Guid>(),
                    UserProfileId = userProfileId,
                    Number = " +48123123123 ",
                    IsMain = true,
                    IsConfirmed = false,
                    IsVisible = true
                },
                AvatarUrl = " https://cdn.example/avatar.png ",
                Bio = " hello ",
                IsActive = true,
                LastSeenAtUtc = new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero)
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserProfileId.Should().Be(userProfileId);
        capturedRequest.FriendlyUserId.Should().Be("john.doe");
        capturedRequest.FirstName.Should().Be("John");
        capturedRequest.LastName.Should().Be("Doe");
        capturedRequest.Organization.Should().Be("FlowChat");
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
                FriendlyUserId = "john.doe",
                MainEmail = new UserProfileEmail
                {
                    Id = _fixture.Create<Guid>(),
                    UserProfileId = Guid.Empty,
                    Address = "john@flowchat.local",
                    IsMain = true,
                    IsAuth = true,
                    IsConfirmed = false,
                    IsVisible = true
                }
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.InsertUserProfileProjectionAsync(It.IsAny<UserProfileProjectionRequest>(), It.IsAny<CancellationToken>()),
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
    public async Task HandleAsync_WhenStateChangedEventArrives_UpdatesProjection()
    {
        UserProfileProjectionRequest? capturedRequest = null;

        _apiClientMock
            .Setup(x => x.UpdateUserProfileProjectionAsync(It.IsAny<UserProfileProjectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new UserProfileStateChangedIntegrationEvent
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = "jane.doe",
                FirstName = "Jane",
                LastName = "Doe",
                Organization = "FlowChat",
                MainEmail = null,
                MainPhone = "123456",
                AvatarUrl = null,
                Bio = "updated",
                IsActive = false,
                LastSeenAtUtc = null
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.FriendlyUserId.Should().Be("jane.doe");
        capturedRequest.FirstName.Should().Be("Jane");
        capturedRequest.LastName.Should().Be("Doe");
        capturedRequest.Organization.Should().Be("FlowChat");
        capturedRequest.IsActive.Should().BeFalse();
        capturedRequest.MainPhone.Should().Be("123456");
    }
}
