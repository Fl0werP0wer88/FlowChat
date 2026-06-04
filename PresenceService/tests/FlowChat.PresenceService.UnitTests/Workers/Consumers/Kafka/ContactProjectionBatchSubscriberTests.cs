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

public sealed class ContactProjectionBatchSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceInternalApiClient> _apiClientMock = new();
    private readonly ContactProjectionBatchSubscriber _subscriber;

    public ContactProjectionBatchSubscriberTests()
    {
        _subscriber = new ContactProjectionBatchSubscriber(
            _apiClientMock.Object,
            NullLogger<ContactProjectionBatchSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedAndUpdatedProjectionEventsArrive_SendsSingleBulkRequest()
    {
        BulkUpsertOrDeleteUserContactProjectionRequest? capturedRequest = null;
        var createdOwnerUserId = _fixture.Create<Guid>();
        var createdContactUserId = _fixture.Create<Guid>();
        var updatedOwnerUserId = _fixture.Create<Guid>();
        var updatedContactUserId = _fixture.Create<Guid>();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(createdOwnerUserId, createdContactUserId, OperationType.Created, 1),
                CreateProjectionEvent(updatedOwnerUserId, updatedContactUserId, OperationType.Updated, 3)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().HaveCount(2);

        var createdItem = capturedRequest.Items.Should().ContainSingle(x =>
            x.ObserverUserId == createdOwnerUserId &&
            x.ObservedUserId == createdContactUserId).Subject;
        createdItem.SourceVersion.Should().Be(1);
        createdItem.Value.Should().NotBeNull();
        createdItem.Value!.Source.Should().Be("social-graph-contact-events");

        var updatedItem = capturedRequest.Items.Should().ContainSingle(x =>
            x.ObserverUserId == updatedOwnerUserId &&
            x.ObservedUserId == updatedContactUserId).Subject;
        updatedItem.SourceVersion.Should().Be(3);
        updatedItem.Value.Should().NotBeNull();
        updatedItem.Value!.Source.Should().Be("social-graph-contact-events");
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_SendsItemWithNullValueAndSourceVersion()
    {
        BulkUpsertOrDeleteUserContactProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 4)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.ObservedUserId.Should().Be(contactUserId);
        item.ObserverUserId.Should().Be(ownerUserId);
        item.SourceVersion.Should().Be(4);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsDuplicateContactObserverKey_SendsHighestVersionItem()
    {
        BulkUpsertOrDeleteUserContactProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Updated, 5),
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 2)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.ObservedUserId.Should().Be(contactUserId);
        item.ObserverUserId.Should().Be(ownerUserId);
        item.SourceVersion.Should().Be(5);
        item.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchContainsSameVersionDuplicate_SendsLastItem()
    {
        BulkUpsertOrDeleteUserContactProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        SetupCaptureRequest(request => capturedRequest = request);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Updated, 5),
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted, 5)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var item = capturedRequest!.Items.Should().ContainSingle().Subject;
        item.SourceVersion.Should().Be(5);
        item.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotCallApi()
    {
        await _subscriber.HandleAsync(ToAsyncEnumerable(), CancellationToken.None);

        _apiClientMock.Verify(
            x => x.BulkUpsertOrDeleteUserContactProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenOneEventIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(Guid.NewGuid(), Guid.NewGuid(), OperationType.Created, 1),
                CreateProjectionEvent(Guid.Empty, Guid.NewGuid(), OperationType.Updated, 2)),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        _apiClientMock.Verify(
            x => x.BulkUpsertOrDeleteUserContactProjectionAsync(
                It.IsAny<BulkUpsertOrDeleteUserContactProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenVersionIsInvalid_ThrowsAndDoesNotCallApi()
    {
        var act = () => _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(Guid.NewGuid(), Guid.NewGuid(), OperationType.Updated, 0)),
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
