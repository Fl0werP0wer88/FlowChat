using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileProjectionRetrySubscriberTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly UserProfileProjectionRetrySubscriber _subscriber;

    public UserProfileProjectionRetrySubscriberTests()
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

        _subscriber = new UserProfileProjectionRetrySubscriber(
            _mediatorMock.Object,
            _mapper,
            NullLogger<UserProfileProjectionRetrySubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedProjectionEventArrives_SendsSingleItemBulkCommand()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(userProfileId, OperationType.Created, 7, friendlyUserId: " john.doe ", firstName: " John "),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.EntityId.Value.Should().Be(userProfileId);
        item.SourceVersion.Should().Be(7);
        item.Value.Should().NotBeNull();
        item.Value!.FriendlyUserId.Should().Be("john.doe");
        item.Value.FirstName.Should().Be("John");
        item.Value.SourceVersion.Should().Be(7);
        item.Value.Source.Should().Be("user-profile-projection");
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_SendsSingleDeleteItem()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureCommand(command => capturedCommand = command);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.Empty, OperationType.Deleted, 4, sourceAggregateId: userProfileId),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.EntityId.Value.Should().Be(userProfileId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenProjectionEventIsInvalid_ThrowsAndDoesNotSendCommand()
    {
        var act = () => _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.Empty, OperationType.Updated, 2),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandFailsWithIsolableFailure_ThrowsIsolableException()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("bulk failed", FailureKind.Isolable)));

        var act = () => _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.NewGuid(), OperationType.Updated, 3),
            CancellationToken.None);

        await act.Should().ThrowAsync<IsolableException>()
            .WithMessage("bulk failed");
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
                IsActive = true
            }
        };
}
