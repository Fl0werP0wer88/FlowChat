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

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka;

public sealed class ContactProjectionRetrySubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ContactProjectionRetrySubscriber _subscriber;

    public ContactProjectionRetrySubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new ContactProjectionRetrySubscriber(
            _mediatorMock.Object,
            NullLogger<ContactProjectionRetrySubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedProjectionEventArrives_SendsSingleItemBulkCommand()
    {
        BulkUpsertOrDeleteUserContactProjectionCommand? capturedCommand = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Created, 7),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.ObserverUserId.Should().Be(ownerUserId);
        item.ObservedUserId.Should().Be(contactUserId);
        item.SourceVersion.Should().Be(7);
        item.Value.Should().NotBeNull();
        item.Value!.Source.Should().Be("social-graph-contact-events");
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_SendsSingleDeleteItem()
    {
        BulkUpsertOrDeleteUserContactProjectionCommand? capturedCommand = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 4),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.ObserverUserId.Should().Be(ownerUserId);
        item.ObservedUserId.Should().Be(contactUserId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenProjectionEventIsInvalid_ThrowsAndDoesNotSendCommand()
    {
        var act = () => _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.Empty, Guid.NewGuid(), OperationType.Updated, 2),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithIsolableFailure_ThrowsIsolableException()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed", FailureKind.Isolable)));

        var act = () => _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.NewGuid(), Guid.NewGuid(), OperationType.Updated, 3),
            CancellationToken.None);

        await act.Should().ThrowAsync<IsolableException>()
            .WithMessage("bulk failed");
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
}
