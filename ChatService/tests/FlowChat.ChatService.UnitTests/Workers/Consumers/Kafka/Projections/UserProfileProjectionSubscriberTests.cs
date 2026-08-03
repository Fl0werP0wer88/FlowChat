using AutoMapper;
using Confluent.Kafka;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Consumers.Kafka;
using FlowChat.ChatService.Consumers.Kafka.Projections;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;

namespace FlowChat.ChatService.UnitTests.Workers.Consumers.Kafka.Projections;

public sealed class UserProfileProjectionSubscriberTests
{
    private readonly IMapper _mapper;

    public UserProfileProjectionSubscriberTests()
    {
        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile<UserProfileProjectionRequestProfile>(),
            NullLoggerFactory.Instance);
        _mapper = mapperConfiguration.CreateMapper();
    }

    [Fact]
    public async Task HandleAsync_UpdatedEvent_MapsReadModelAndSendsProjectionCommand()
    {
        var userProfileId = Guid.NewGuid();
        var message = CreateProjectionEvent(
            OperationType.Updated,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "jdoe",
                FirstName = "John",
                LastName = "Doe",
                AvatarUrl = "https://avatar",
                MainEmail = new UserProfileEmail
                {
                    Address = "jdoe@example.com",
                    IsConfirmed = true,
                    IsVisible = false
                }
            });

        var command = await HandleAndCaptureCommandAsync(message);

        command.Item.Value.UserProfileId.Should().Be(userProfileId);
        command.Item.Value.FriendlyUserId.Should().Be("jdoe");
        command.Item.Value.FirstName.Should().Be("John");
        command.Item.Value.LastName.Should().Be("Doe");
        command.Item.Value.AvatarUrl.Should().Be("https://avatar");
        command.Item.Value.Email.Should().Be("jdoe@example.com");
        command.Item.Value.Source.Should().Be("user-profile-projection");
    }

    [Fact]
    public async Task HandleAsync_EventWithoutMainEmail_MapsEmailAsNull()
    {
        var userProfileId = Guid.NewGuid();
        var message = CreateProjectionEvent(
            OperationType.Updated,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "jdoe"
            });

        var command = await HandleAndCaptureCommandAsync(message);

        command.Item.Value.Email.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_DeletedEvent_MapsFullPayloadAndDeletionMetadata()
    {
        var userProfileId = Guid.NewGuid();
        var deletedAt = DateTimeOffset.UtcNow;
        var message = CreateProjectionEvent(
            OperationType.Deleted,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "deleted-user",
                FirstName = "Deleted",
                LastName = "Profile"
            },
            deletedAt);

        var command = await HandleAndCaptureCommandAsync(message);

        command.Item.Value.UserProfileId.Should().Be(userProfileId);
        command.Item.Value.FriendlyUserId.Should().Be("deleted-user");
        command.Item.Value.FirstName.Should().Be("Deleted");
        command.Item.Value.LastName.Should().Be("Profile");
        command.Item.Value.Source.Should().Be("user-profile-projection");
        command.Item.Operation.Should().Be(OperationType.Deleted);
        command.Item.SourceDeletedAtUtc.Should().Be(deletedAt);
    }

    [Fact]
    public async Task HandleAsync_EventWithInvalidFriendlyUserId_ThrowsNonTransientException()
    {
        var userProfileId = Guid.NewGuid();
        var subscriber = CreateSubscriber(new Mock<IMediator>());
        var message = CreateProjectionEvent(
            OperationType.Updated,
            userProfileId,
            new UserProfileReadModel
            {
                UserProfileId = userProfileId,
                FriendlyUserId = "   "
            });

        var action = () => subscriber.HandleAsync(CreateEnvelope(message), CancellationToken.None);

        await action.Should().ThrowAsync<NonTransientException>();
    }

    private async Task<ProjectionSingleCommand<UserProfileProjectionDto>> HandleAndCaptureCommandAsync(
        ProjectionIntegrationEvent<UserProfileReadModel> message)
    {
        ProjectionSingleCommand<UserProfileProjectionDto>? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionSingleCommand<UserProfileProjectionDto>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((command, _) =>
                capturedCommand = (ProjectionSingleCommand<UserProfileProjectionDto>)command)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await CreateSubscriber(mediatorMock).HandleAsync(CreateEnvelope(message), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        return capturedCommand!;
    }

    private UserProfileProjectionSubscriber CreateSubscriber(Mock<IMediator> mediatorMock) =>
        new(
            mediatorMock.Object,
            Mock.Of<IConsumedOffsetCommitter>(),
            _mapper,
            NullLogger<UserProfileProjectionSubscriber>.Instance);

    private static ProjectionIntegrationEvent<UserProfileReadModel> CreateProjectionEvent(
        OperationType operation,
        Guid sourceAggregateId,
        UserProfileReadModel value,
        DateTimeOffset? deletedAt = null) =>
        new()
        {
            SourceAggregateId = sourceAggregateId,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateDeletedAt = deletedAt,
            Operation = operation,
            SourceAggregateVersion = 1,
            Value = value
        };

    private static IInboundEnvelope<ProjectionIntegrationEvent<UserProfileReadModel>> CreateEnvelope(
        ProjectionIntegrationEvent<UserProfileReadModel> message)
    {
        var envelopeMock = new Mock<IInboundEnvelope<ProjectionIntegrationEvent<UserProfileReadModel>>>();
        envelopeMock.SetupGet(envelope => envelope.Message).Returns(message);
        envelopeMock.SetupGet(envelope => envelope.Headers).Returns(new MessageHeaderCollection(0));
        envelopeMock
            .SetupGet(envelope => envelope.Endpoint)
            .Returns(new KafkaConsumerEndpoint(
                "dev.flowchat.user-profile.user-profile-projection.v1",
                Partition.Any,
                new KafkaConsumerEndpointConfiguration()));

        return envelopeMock.Object;
    }
}
