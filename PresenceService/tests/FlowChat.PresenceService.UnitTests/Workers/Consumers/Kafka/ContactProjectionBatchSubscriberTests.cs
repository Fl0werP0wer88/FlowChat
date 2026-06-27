using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka;

public sealed class ContactProjectionBatchSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly ContactProjectionBatchSubscriber _subscriber;

    public ContactProjectionBatchSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new ContactProjectionBatchSubscriber(
            _mediatorMock.Object,
            _publisherMock.Object,
            NullLogger<ContactProjectionBatchSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedAndUpdatedProjectionEventsArrive_SendsSingleBulkCommand()
    {
        BulkUpsertOrDeleteUserContactProjectionCommand? capturedCommand = null;
        var createdOwnerUserId = _fixture.Create<Guid>();
        var createdContactUserId = _fixture.Create<Guid>();
        var updatedOwnerUserId = _fixture.Create<Guid>();
        var updatedContactUserId = _fixture.Create<Guid>();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(createdOwnerUserId, createdContactUserId, OperationType.Created, 1),
                CreateProjectionEvent(updatedOwnerUserId, updatedContactUserId, OperationType.Updated, 3)),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().HaveCount(2);

        var createdItem = capturedCommand.Items.Should().ContainSingle(x =>
            x.ObserverUserId == createdOwnerUserId &&
            x.ObservedUserId == createdContactUserId).Subject;
        createdItem.SourceVersion.Should().Be(1);
        createdItem.Value.Should().NotBeNull();
        createdItem.Value!.Source.Should().Be("social-graph-contact-events");

        var updatedItem = capturedCommand.Items.Should().ContainSingle(x =>
            x.ObserverUserId == updatedOwnerUserId &&
            x.ObservedUserId == updatedContactUserId).Subject;
        updatedItem.SourceVersion.Should().Be(3);
        updatedItem.Value.Should().NotBeNull();
        updatedItem.Value!.Source.Should().Be("social-graph-contact-events");
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_SendsItemWithNullValueAndSourceVersion()
    {
        BulkUpsertOrDeleteUserContactProjectionCommand? capturedCommand = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 4)),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.ObservedUserId.Should().Be(contactUserId);
        item.ObserverUserId.Should().Be(ownerUserId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsDuplicateContactObserverKey_SendsHighestVersionItem()
    {
        BulkUpsertOrDeleteUserContactProjectionCommand? capturedCommand = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Updated, 5),
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 2)),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.ObservedUserId.Should().Be(contactUserId);
        item.ObserverUserId.Should().Be(ownerUserId);
        item.SourceVersion.Should().Be(5);
        item.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsSameVersionDuplicate_SendsLastItem()
    {
        BulkUpsertOrDeleteUserContactProjectionCommand? capturedCommand = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Updated, 5),
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 5)),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.SourceVersion.Should().Be(5);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotSendCommand()
    {
        await _subscriber.HandleAsync(ToAsyncEnumerable(), CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
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
                CreateProjectionEvent(Guid.NewGuid(), Guid.NewGuid(), OperationType.Created, 1),
                CreateProjectionEvent(Guid.Empty, Guid.NewGuid(), OperationType.Updated, 2)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenVersionIsInvalid_ThrowsAndDoesNotSendCommand()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(Guid.NewGuid(), Guid.NewGuid(), OperationType.Updated, 0)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithIsolableFailure_PublishesOriginalMessagesToRetry()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed", FailureKind.Isolable)));
        _publisherMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var messages = new[]
        {
            CreateProjectionEvent(Guid.NewGuid(), Guid.NewGuid(), OperationType.Updated, 1),
            CreateProjectionEvent(Guid.NewGuid(), Guid.NewGuid(), OperationType.Updated, 2)
        };

        await _subscriber.HandleAsync(ToAsyncEnumerable(messages), CancellationToken.None);

        foreach (var message in messages)
        {
            _publisherMock.Verify(
                x => x.PublishAsync(message, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    private void SetupCaptureCommand(Action<BulkUpsertOrDeleteUserContactProjectionCommand> capture)
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((command, _) => capture((BulkUpsertOrDeleteUserContactProjectionCommand)command))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
    }

    private ProjectionIntegrationEvent<ContactReadModel> CreateProjectionEvent(
        Guid ownerUserId,
        Guid contactUserId,
        OperationType operation,
        int version) =>
        new()
        {
            SourceAggregateId = _fixture.Create<Guid>(),
            SourceAggregateCreatedAtUtc = new DateTimeOffset(2026, 6, 5, 10, 0, 0, TimeSpan.Zero),
            SourceAggregateModifiedAtUtc = new DateTimeOffset(2026, 6, 5, 10, 5, 0, TimeSpan.Zero),
            SourceAggregateDeletedAt = operation == OperationType.Deleted
                ? new DateTimeOffset(2026, 6, 5, 10, 10, 0, TimeSpan.Zero)
                : null,
            Operation = operation,
            SourceAggregateVersion = version,
            Value = new ContactReadModel
            {
                ContactId = _fixture.Create<Guid>(),
                OwnerUserId = ownerUserId,
                ContactUserId = contactUserId,
                DisplayName = _fixture.Create<string>(),
                FirstName = _fixture.Create<string>(),
                LastName = _fixture.Create<string>(),
                PhoneNumber = "+48123123123",
                EmailAddress = "contact@example.com",
                IsBlocked = false
            }
        };

    private static async IAsyncEnumerable<ProjectionIntegrationEvent<ContactReadModel>> ToAsyncEnumerable(
        params ProjectionIntegrationEvent<ContactReadModel>[] messages)
    {
        foreach (var message in messages)
        {
            yield return message;
            await Task.Yield();
        }
    }
}
