using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileProjectionBatchSubscriberTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ISocialGraphInternalApiClient> _apiClientMock = new();
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
    public async Task HandleAsync_WhenCreatedAndUpdatedProjectionEventsArrive_SendsSingleBulkRequest()
    {
        BulkUpsertOrDeleteUserProfileProjectionRequest? capturedRequest = null;
        var createdUserProfileId = Guid.NewGuid();
        var updatedUserProfileId = Guid.NewGuid();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(
                    createdUserProfileId,
                    OperationType.Created,
                    1,
                    friendlyUserId: " john.doe ",
                    firstName: " John ",
                    lastName: " Doe ",
                    organization: " FlowChat ",
                    mainEmail: new UserProfileEmail
                    {
                        Address = " john@flowchat.local ",
                        IsConfirmed = true,
                        IsVisible = true
                    },
                    mainPhone: new UserProfilePhone
                    {
                        Number = " +48123123123 ",
                        IsConfirmed = false,
                        IsVisible = true
                    },
                    avatarUrl: " https://cdn.example/john.png ",
                    bio: " hello "),
                CreateProjectionEvent(
                    updatedUserProfileId,
                    OperationType.Updated,
                    3,
                    friendlyUserId: " jane.doe ",
                    firstName: " Jane ",
                    lastName: null,
                    organization: " ",
                    mainEmail: null,
                    mainPhone: null,
                    avatarUrl: " ",
                    bio: null,
                    isActive: false)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().HaveCount(2);

        var createdItem = capturedRequest.Items.Should().ContainSingle(x => x.UserProfileId == createdUserProfileId).Subject;
        createdItem.SourceVersion.Should().Be(1);
        createdItem.Value.Should().NotBeNull();
        createdItem.Value!.FriendlyUserId.Should().Be("john.doe");
        createdItem.Value.FirstName.Should().Be("John");
        createdItem.Value.LastName.Should().Be("Doe");
        createdItem.Value.Organization.Should().Be("FlowChat");
        createdItem.Value.MainEmailAddress.Should().Be("john@flowchat.local");
        createdItem.Value.MainEmailIsConfirmed.Should().BeTrue();
        createdItem.Value.MainEmailIsVisible.Should().BeTrue();
        createdItem.Value.MainPhoneNumber.Should().Be("+48123123123");
        createdItem.Value.MainPhoneIsConfirmed.Should().BeFalse();
        createdItem.Value.MainPhoneIsVisible.Should().BeTrue();
        createdItem.Value.AvatarUrl.Should().Be("https://cdn.example/john.png");
        createdItem.Value.Bio.Should().Be("hello");
        createdItem.Value.SourceVersion.Should().Be(1);
        createdItem.Value.Source.Should().Be("user-profile-projection");

        var updatedItem = capturedRequest.Items.Should().ContainSingle(x => x.UserProfileId == updatedUserProfileId).Subject;
        updatedItem.SourceVersion.Should().Be(3);
        updatedItem.Value.Should().NotBeNull();
        updatedItem.Value!.FriendlyUserId.Should().Be("jane.doe");
        updatedItem.Value.FirstName.Should().Be("Jane");
        updatedItem.Value.Organization.Should().BeNull();
        updatedItem.Value.MainEmailAddress.Should().BeNull();
        updatedItem.Value.MainPhoneNumber.Should().BeNull();
        updatedItem.Value.AvatarUrl.Should().BeNull();
        updatedItem.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_SendsItemWithNullValueAndSourceVersion()
    {
        BulkUpsertOrDeleteUserProfileProjectionRequest? capturedRequest = null;
        var deletedUserProfileId = Guid.NewGuid();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(deletedUserProfileId, OperationType.Deleted, 4)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.UserProfileId.Should().Be(deletedUserProfileId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventHasEmptyBodyId_UsesSourceAggregateId()
    {
        BulkUpsertOrDeleteUserProfileProjectionRequest? capturedRequest = null;
        var deletedUserProfileId = Guid.NewGuid();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(Guid.Empty, OperationType.Deleted, 4, sourceAggregateId: deletedUserProfileId)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.UserProfileId.Should().Be(deletedUserProfileId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsDuplicateUserProfileId_SendsHighestVersionItem()
    {
        BulkUpsertOrDeleteUserProfileProjectionRequest? capturedRequest = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(userProfileId, OperationType.Updated, 5, friendlyUserId: "johnny.doe", firstName: "Johnny"),
                CreateProjectionEvent(userProfileId, OperationType.Updated, 2, friendlyUserId: "john.doe", firstName: "John")),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.UserProfileId.Should().Be(userProfileId);
        item.SourceVersion.Should().Be(5);
        item.Value.Should().NotBeNull();
        item.Value!.FriendlyUserId.Should().Be("johnny.doe");
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsSameVersionDuplicate_SendsLastItem()
    {
        BulkUpsertOrDeleteUserProfileProjectionRequest? capturedRequest = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(userProfileId, OperationType.Updated, 5, friendlyUserId: "john.doe"),
                CreateProjectionEvent(userProfileId, OperationType.Deleted, 5)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.UserProfileId.Should().Be(userProfileId);
        item.SourceVersion.Should().Be(5);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotCallApi()
    {
        await _subscriber.HandleAsync(ToAsyncEnumerable(), CancellationToken.None);

        _apiClientMock.Verify(
            x => x.BulkUpsertOrDeleteUserProfileProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOneEventIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(Guid.NewGuid(), OperationType.Created, 1),
                CreateProjectionEvent(Guid.Empty, OperationType.Updated, 2)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.BulkUpsertOrDeleteUserProfileProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenVersionIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(Guid.NewGuid(), OperationType.Updated, 0)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.BulkUpsertOrDeleteUserProfileProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenFriendlyUserIdIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(Guid.NewGuid(), OperationType.Updated, 1, friendlyUserId: " ")),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.BulkUpsertOrDeleteUserProfileProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupCaptureRequest(Action<BulkUpsertOrDeleteUserProfileProjectionRequest> capture)
    {
        _apiClientMock
            .Setup(x => x.BulkUpsertOrDeleteUserProfileProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<BulkUpsertOrDeleteUserProfileProjectionRequest, CancellationToken>((request, _) => capture(request))
            .Returns(Task.CompletedTask);
    }

    private static ProjectionIntegrationEvent<UserProfileReadModel> CreateProjectionEvent(
        Guid userProfileId,
        OperationType operation,
        int version,
        string friendlyUserId = "john.doe",
        string? firstName = "John",
        string? lastName = "Doe",
        string? organization = null,
        UserProfileEmail? mainEmail = null,
        UserProfilePhone? mainPhone = null,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        DateTimeOffset? lastSeenAtUtc = null,
        Guid? sourceAggregateId = null) =>
        new()
        {
            SourceAggregateId = sourceAggregateId ?? userProfileId,
            SourceAggregateCreatedAtUtc = new DateTimeOffset(2026, 6, 5, 10, 0, 0, TimeSpan.Zero),
            SourceAggregateModifiedAtUtc = new DateTimeOffset(2026, 6, 5, 10, 5, 0, TimeSpan.Zero),
            SourceAggregateDeletedAt = operation == OperationType.Deleted
                ? new DateTimeOffset(2026, 6, 5, 10, 10, 0, TimeSpan.Zero)
                : null,
            Operation = operation,
            SourceAggregateVersion = version,
            Value = new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = friendlyUserId,
                FirstName = firstName,
                LastName = lastName,
                Organization = organization,
                MainEmail = mainEmail,
                MainPhone = mainPhone,
                AvatarUrl = avatarUrl,
                Bio = bio,
                IsActive = isActive,
                LastSeenAtUtc = lastSeenAtUtc
            }
        };

    private static async IAsyncEnumerable<ProjectionIntegrationEvent<UserProfileReadModel>> ToAsyncEnumerable(
        params ProjectionIntegrationEvent<UserProfileReadModel>[] messages)
    {
        foreach (var message in messages)
        {
            yield return message;
            await Task.Yield();
        }
    }
}
