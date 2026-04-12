using AutoFixture;
using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.PresenceService.Consumers.Kafka;
using FlowChat.PresenceService.Consumers.Presence.Contracts;
using FlowChat.PresenceService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class ContactAddedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceInternalApiClient> _apiClientMock = new();
    private readonly ContactAddedSubscriber _subscriber;

    public ContactAddedSubscriberTests()
    {
        _subscriber = new ContactAddedSubscriber(
            _apiClientMock.Object,
            NullLogger<ContactAddedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenContactAdded_InsertsContactObserverProjection()
    {
        ContactObserverProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.InsertContactObserverProjectionAsync(It.IsAny<ContactObserverProjectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ContactObserverProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new ContactAddedIntegrationEvent
            {
                Key = _fixture.Create<string>(),
                OwnerUserId = ownerUserId,
                ContactUserId = contactUserId
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.ObservedUserId.Should().Be(contactUserId);
        capturedRequest.ObserverUserId.Should().Be(ownerUserId);
    }
}

public sealed class ContactDeletedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceInternalApiClient> _apiClientMock = new();
    private readonly ContactDeletedSubscriber _subscriber;

    public ContactDeletedSubscriberTests()
    {
        _subscriber = new ContactDeletedSubscriber(
            _apiClientMock.Object,
            NullLogger<ContactDeletedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WhenContactDeleted_DeletesContactObserverProjection()
    {
        ContactObserverProjectionRequest? capturedRequest = null;
        var ownerUserId = _fixture.Create<Guid>();
        var contactUserId = _fixture.Create<Guid>();

        _apiClientMock
            .Setup(x => x.DeleteContactObserverProjectionAsync(It.IsAny<ContactObserverProjectionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ContactObserverProjectionRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);

        await _subscriber.HandleAsync(
            new ContactDeletedIntegrationEvent
            {
                Key = _fixture.Create<string>(),
                OwnerUserId = ownerUserId,
                ContactUserId = contactUserId
            },
            CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.ObservedUserId.Should().Be(contactUserId);
        capturedRequest.ObserverUserId.Should().Be(ownerUserId);
    }
}
