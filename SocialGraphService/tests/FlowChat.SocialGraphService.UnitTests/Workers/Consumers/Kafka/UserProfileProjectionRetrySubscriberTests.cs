using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UserProfileProjectionRetrySubscriberTests
{
    private readonly IMapper _mapper;
    private readonly Mock<ISocialGraphInternalApiClient> _apiClientMock = new();
    private readonly UserProfileProjectionRetrySubscriber _subscriber;

    public UserProfileProjectionRetrySubscriberTests()
    {
        _mapper = new MapperConfiguration(
                configuration => configuration.AddProfile<UserProfileProjectionRequestProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        _subscriber = new UserProfileProjectionRetrySubscriber(
            _apiClientMock.Object,
            _mapper,
            NullLogger<UserProfileProjectionRetrySubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedProjectionEventArrives_SendsSingleItemBulkRequest()
    {
        BulkUpsertOrDeleteUserProfileProjectionRequest? capturedRequest = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(userProfileId, OperationType.Created, 7, friendlyUserId: " john.doe ", firstName: " John "),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.UserProfileId.Should().Be(userProfileId);
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
        BulkUpsertOrDeleteUserProfileProjectionRequest? capturedRequest = null;
        var userProfileId = Guid.NewGuid();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.Empty, OperationType.Deleted, 4, sourceAggregateId: userProfileId),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.UserProfileId.Should().Be(userProfileId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenProjectionEventIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.Empty, OperationType.Updated, 2),
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
        Guid? sourceAggregateId = null) =>
        new()
        {
            SourceAggregateId = sourceAggregateId ?? userProfileId,
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
