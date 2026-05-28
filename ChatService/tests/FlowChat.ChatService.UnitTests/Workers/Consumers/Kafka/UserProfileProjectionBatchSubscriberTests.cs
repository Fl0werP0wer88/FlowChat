using AutoMapper;
using FlowChat.ChatService.Consumers.ChatService.Contracts;
using FlowChat.ChatService.Consumers.Kafka;
using FlowChat.ChatService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Workers.Consumers.Kafka;

public sealed class UserProfileProjectionBatchSubscriberTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IChatInternalApiClient> _apiClientMock = new();
    private readonly UserProfileProjectionBatchSubscriber _subscriber;

    public UserProfileProjectionBatchSubscriberTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<UserProfileProjectionRequestProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _subscriber = new UserProfileProjectionBatchSubscriber(
            _apiClientMock.Object,
            _mapper,
            NullLogger<UserProfileProjectionBatchSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedAndChangedEventsArrive_SendsSingleBulkUpsertRequest()
    {
        BulkUpsertUserProfileProjectionRequest? capturedRequest = null;
        var createdUserProfileId = Guid.NewGuid();
        var changedUserProfileId = Guid.NewGuid();

        _apiClientMock
            .Setup(x => x.BulkUpsertUserProfileProjectionAsync(
                It.IsAny<BulkUpsertUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<BulkUpsertUserProfileProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                new UserProfileCreatedIntegrationEvent
                {
                    UserProfileId = createdUserProfileId,
                    FriendlyUserId = " john.doe ",
                    FirstName = " John ",
                    LastName = " Doe ",
                    MainEmail = new UserProfileEmail
                    {
                        Address = "john@flowchat.local",
                        IsConfirmed = true,
                        IsVisible = true
                    },
                    AvatarUrl = " https://cdn.example/john.png "
                },
                new UserProfileChangedIntegrationEvent
                {
                    UserProfileId = changedUserProfileId,
                    FriendlyUserId = " jane.doe ",
                    FirstName = " Jane ",
                    LastName = null,
                    AvatarUrl = " "
                }),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().HaveCount(2);

        var createdItem = capturedRequest.Items.Should().ContainSingle(x => x.UserProfileId == createdUserProfileId).Subject;
        createdItem.FriendlyUserId.Should().Be("john.doe");
        createdItem.DisplayName.Should().Be("John Doe");
        createdItem.AvatarUrl.Should().Be("https://cdn.example/john.png");
        createdItem.Source.Should().Be("user-profile-events");

        var changedItem = capturedRequest.Items.Should().ContainSingle(x => x.UserProfileId == changedUserProfileId).Subject;
        changedItem.FriendlyUserId.Should().Be("jane.doe");
        changedItem.DisplayName.Should().Be("Jane");
        changedItem.AvatarUrl.Should().BeNull();
        changedItem.Source.Should().Be("user-profile-events");
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsDuplicateUserProfileId_SendsLastMappedItem()
    {
        BulkUpsertUserProfileProjectionRequest? capturedRequest = null;
        var userProfileId = Guid.NewGuid();

        _apiClientMock
            .Setup(x => x.BulkUpsertUserProfileProjectionAsync(
                It.IsAny<BulkUpsertUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<BulkUpsertUserProfileProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                new UserProfileCreatedIntegrationEvent
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = " john.doe ",
                    FirstName = " John ",
                    LastName = " Doe ",
                    MainEmail = new UserProfileEmail
                    {
                        Address = "john@flowchat.local",
                        IsConfirmed = true,
                        IsVisible = true
                    },
                    AvatarUrl = " https://cdn.example/john-created.png "
                },
                new UserProfileChangedIntegrationEvent
                {
                    UserProfileId = userProfileId,
                    FriendlyUserId = " johnny.doe ",
                    FirstName = " Johnny ",
                    LastName = " Doe ",
                    AvatarUrl = " https://cdn.example/john-changed.png "
                }),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.UserProfileId.Should().Be(userProfileId);
        item.FriendlyUserId.Should().Be("johnny.doe");
        item.DisplayName.Should().Be("Johnny Doe");
        item.AvatarUrl.Should().Be("https://cdn.example/john-changed.png");
    }

    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotCallApi()
    {
        await _subscriber.HandleAsync(ToAsyncEnumerable(), CancellationToken.None);

        _apiClientMock.Verify(
            x => x.BulkUpsertUserProfileProjectionAsync(
                It.IsAny<BulkUpsertUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsOnlyUnsupportedEvent_DoesNotCallApi()
    {
        await _subscriber.HandleAsync(ToAsyncEnumerable(new UnsupportedIntegrationEvent()), CancellationToken.None);

        _apiClientMock.Verify(
            x => x.BulkUpsertUserProfileProjectionAsync(
                It.IsAny<BulkUpsertUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOneEventIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(
                new UserProfileCreatedIntegrationEvent
                {
                    UserProfileId = Guid.NewGuid(),
                    FriendlyUserId = "john.doe",
                    MainEmail = new UserProfileEmail
                    {
                        Address = "john@flowchat.local",
                        IsConfirmed = true,
                        IsVisible = true
                    }
                },
                new UserProfileChangedIntegrationEvent
                {
                    UserProfileId = Guid.Empty,
                    FriendlyUserId = "jane.doe"
                }),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.BulkUpsertUserProfileProjectionAsync(
                It.IsAny<BulkUpsertUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static async IAsyncEnumerable<IntegrationEvent> ToAsyncEnumerable(params IntegrationEvent[] messages)
    {
        foreach (var message in messages)
        {
            yield return message;
            await Task.Yield();
        }
    }

    private sealed record UnsupportedIntegrationEvent : IntegrationEvent;
}
