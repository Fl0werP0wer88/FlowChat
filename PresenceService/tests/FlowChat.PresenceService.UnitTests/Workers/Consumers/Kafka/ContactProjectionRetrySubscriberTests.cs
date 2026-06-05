using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Consumers.Kafka;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka;

public sealed class ContactProjectionRetrySubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceInternalApiClient> _apiClientMock = new();
    private readonly ContactProjectionRetrySubscriber _subscriber;

    public ContactProjectionRetrySubscriberTests()
    {
        _subscriber = new ContactProjectionRetrySubscriber(
            _apiClientMock.Object,
            NullLogger<ContactProjectionRetrySubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedProjectionEventArrives_SendsSingleItemBulkRequest()
    {
        BulkUpsertOrDeleteUserContactProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Created, 7),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.ObserverUserId.Should().Be(ownerUserId);
        item.ObservedUserId.Should().Be(contactUserId);
        item.SourceVersion.Should().Be(7);
        item.Value.Should().NotBeNull();
        item.Value!.Source.Should().Be("social-graph-contact-events");
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_SendsSingleDeleteItem()
    {
        BulkUpsertOrDeleteUserContactProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 4),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.ObserverUserId.Should().Be(ownerUserId);
        item.ObservedUserId.Should().Be(contactUserId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenProjectionEventIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            CreateProjectionEvent(Guid.Empty, Guid.NewGuid(), OperationType.Updated, 2),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.BulkUpsertOrDeleteUserContactProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupCaptureRequest(Action<BulkUpsertOrDeleteUserContactProjectionRequest> capture)
    {
        _apiClientMock
            .Setup(x => x.BulkUpsertOrDeleteUserContactProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<BulkUpsertOrDeleteUserContactProjectionRequest, CancellationToken>((request, _) => capture(request))
            .Returns(Task.CompletedTask);
    }

    private ProjectionIntegrationEvent<ContactReadModel> CreateProjectionEvent(
        Guid ownerUserId,
        Guid contactUserId,
        OperationType operation,
        int version) =>
        new()
        {
            SourceAggregateId = _fixture.Create<Guid>(),
            Operation = operation,
            Version = version,
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
