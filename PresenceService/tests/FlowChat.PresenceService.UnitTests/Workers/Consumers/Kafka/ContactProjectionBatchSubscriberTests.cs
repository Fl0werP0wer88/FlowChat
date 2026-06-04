using AutoFixture;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Consumers.Kafka;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

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
    public async Task HandleAsync_WhenCreatedAndUpdatedProjectionEventsArrive_SendsSingleBulkUpsertRequest()
    {
        BulkUpsertContactObserverProjectionRequest? capturedRequest = null;
        var createdOwnerUserId = _fixture.Create<Guid>();
        var createdContactUserId = _fixture.Create<Guid>();
        var updatedOwnerUserId = _fixture.Create<Guid>();
        var updatedContactUserId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.BulkUpsertContactObserverProjectionAsync(
                It.IsAny<BulkUpsertContactObserverProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<BulkUpsertContactObserverProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(createdOwnerUserId, createdContactUserId, OperationType.Created),
                CreateProjectionEvent(updatedOwnerUserId, updatedContactUserId, OperationType.Updated)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Items.Should().HaveCount(2);
        capturedRequest.Items.Should().ContainSingle(x =>
            x.ObserverUserId == createdOwnerUserId &&
            x.ObservedUserId == createdContactUserId);
        capturedRequest.Items.Should().ContainSingle(x =>
            x.ObserverUserId == updatedOwnerUserId &&
            x.ObservedUserId == updatedContactUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedProjectionEventArrives_DeletesContactObserverProjection()
    {
        ContactObserverProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.DeleteContactObserverProjectionAsync(
                It.IsAny<ContactObserverProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<ContactObserverProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted)),
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.ObservedUserId.Should().Be(contactUserId);
        capturedRequest.ObserverUserId.Should().Be(ownerUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenBatchIsEmpty_DoesNotCallApi()
    {
        await _subscriber.HandleAsync(ToAsyncEnumerable(), CancellationToken.None);

        _apiClientMock.Verify(
            x => x.BulkUpsertContactObserverProjectionAsync(
                It.IsAny<BulkUpsertContactObserverProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _apiClientMock.Verify(
            x => x.DeleteContactObserverProjectionAsync(
                It.IsAny<ContactObserverProjectionRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUpsertPrecedesDelete_FlushesUpsertBeforeDelete()
    {
        var calls = new List<string>();
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.BulkUpsertContactObserverProjectionAsync(
                It.IsAny<BulkUpsertContactObserverProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("upsert"))
            .Returns(Task.CompletedTask);
        _apiClientMock
            .Setup(x => x.DeleteContactObserverProjectionAsync(
                It.IsAny<ContactObserverProjectionRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("delete"))
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            ToAsyncEnumerable(
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Created),
                CreateProjectionEvent(ownerUserId, contactUserId, OperationType.Deleted)),
            CancellationToken.None);

        calls.Should().Equal("upsert", "delete");
    }

    private ProjectionIntegrationEvent<ContactReadModel> CreateProjectionEvent(
        Guid ownerUserId,
        Guid contactUserId,
        OperationType operation) =>
        new()
        {
            Operation = operation,
            Version = 1,
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
