using AutoMapper;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.ChatService.Consumers.Kafka;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.ChatService.UnitTests.Workers.Consumers.Kafka;

public sealed class UserProfileProjectionBatchSubscriberTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly UserProfileProjectionBatchSubscriber _subscriber;

    public UserProfileProjectionBatchSubscriberTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<UserProfileProjectionRequestProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new UserProfileProjectionBatchSubscriber(
            _mediatorMock.Object,
            _publisherMock.Object,
            _mapper,
            NullLogger<UserProfileProjectionBatchSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedAndUpdatedProjectionEventsArrive_SendsSingleBulkCommand()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var createdUserProfileId = Guid.NewGuid();
        var updatedUserProfileId = Guid.NewGuid();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(createdUserProfileId, OperationType.Created, 1, " john.doe ", " John ", " Doe ", " https://cdn.example/john.png "),
                CreateProjectionEvent(updatedUserProfileId, OperationType.Updated, 3, " jane.doe ", " Jane ", null, " ")),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().HaveCount(2);

        var createdItem = capturedCommand.Items.Should().ContainSingle(x => x.EntityId.Value == createdUserProfileId).Subject;
        createdItem.SourceVersion.Should().Be(1);
        createdItem.Value.Should().NotBeNull();
        createdItem.Value!.FriendlyUserId.Should().Be("john.doe");
        createdItem.Value.FirstName.Should().Be("John");
        createdItem.Value.LastName.Should().Be("Doe");
        createdItem.Value.AvatarUrl.Should().Be("https://cdn.example/john.png");
        createdItem.Value.SourceVersion.Should().Be(1);
        createdItem.Value.Source.Should().Be("user-profile-projection");

        var updatedItem = capturedCommand.Items.Should().ContainSingle(x => x.EntityId.Value == updatedUserProfileId).Subject;
        updatedItem.SourceVersion.Should().Be(3);
        updatedItem.Value.Should().NotBeNull();
        updatedItem.Value!.FriendlyUserId.Should().Be("jane.doe");
        updatedItem.Value.FirstName.Should().Be("Jane");
        updatedItem.Value.LastName.Should().BeNull();
        updatedItem.Value.AvatarUrl.Should().BeNull();
        updatedItem.Value.SourceVersion.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_SendsItemWithNullValueAndSourceVersion()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var deletedUserProfileId = Guid.NewGuid();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(deletedUserProfileId, OperationType.Deleted, 4)),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.EntityId.Value.Should().Be(deletedUserProfileId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventHasEmptyBodyId_UsesSourceAggregateId()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var deletedUserProfileId = Guid.NewGuid();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(Guid.Empty, OperationType.Deleted, 4, sourceAggregateId: deletedUserProfileId)),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.EntityId.Value.Should().Be(deletedUserProfileId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsDuplicateUserProfileId_SendsHighestVersionItem()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(userProfileId, OperationType.Updated, 5, "johnny.doe", "Johnny"),
                CreateProjectionEvent(userProfileId, OperationType.Updated, 2, "john.doe", "John")),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.EntityId.Value.Should().Be(userProfileId);
        item.SourceVersion.Should().Be(5);
        item.Value.Should().NotBeNull();
        item.Value!.FriendlyUserId.Should().Be("johnny.doe");
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsSameVersionDuplicate_SendsLastItem()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(userProfileId, OperationType.Updated, 5, "john.doe", "John"),
                CreateProjectionEvent(userProfileId, OperationType.Deleted, 5)),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.EntityId.Value.Should().Be(userProfileId);
        item.SourceVersion.Should().Be(5);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotSendCommand()
    {
        await _subscriber.HandleAsync(ToAsyncEnumerable(), CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _publisherMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOneEventIsInvalid_ThrowsAndDoesNotSendCommand()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(Guid.NewGuid(), OperationType.Created, 1),
                CreateProjectionEvent(Guid.Empty, OperationType.Updated, 2)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenVersionIsInvalid_ThrowsAndDoesNotSendCommand()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(Guid.NewGuid(), OperationType.Updated, 0)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithIsolableFailure_PublishesOriginalMessagesToRetry()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed", FailureKind.Isolable)));
        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var messages = new[]
        {
            CreateProjectionEvent(Guid.NewGuid(), OperationType.Updated, 1),
            CreateProjectionEvent(Guid.NewGuid(), OperationType.Updated, 2)
        };

        await _subscriber.HandleAsync(ToAsyncEnumerable(messages), CancellationToken.None);

        foreach (var message in messages)
        {
            _publisherMock.Verify(
                x => x.PublishAsync(message, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    private void SetupCaptureCommand(Action<BulkUpsertOrDeleteUserProfileProjectionCommand> capture)
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((command, _) => capture((BulkUpsertOrDeleteUserProfileProjectionCommand)command))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
    }

    private static ProjectionIntegrationEvent<UserProfileReadModel> CreateProjectionEvent(
        Guid userProfileId,
        OperationType operation,
        int version,
        string friendlyUserId = "john.doe",
        string? firstName = "John",
        string? lastName = "Doe",
        string? avatarUrl = null,
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
                AvatarUrl = avatarUrl
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
