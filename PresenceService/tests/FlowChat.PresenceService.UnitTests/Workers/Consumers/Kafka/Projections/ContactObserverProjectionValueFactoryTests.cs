using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Consumers.Kafka.Projections;
using FluentAssertions;

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionValueFactoryTests
{
    private readonly ContactObserverProjectionValueFactory _factory = new();

    [Fact]
    public void MapValue_WhenEventIsValid_MapsContactUserIdAndOwnerUserId()
    {
        var ownerUserId = Guid.NewGuid();
        var contactUserId = Guid.NewGuid();

        var value = _factory.MapValue(CreateProjectionEvent(ownerUserId, contactUserId));

        value.ObserverUserId.Should().Be(ownerUserId);
        value.ObservedUserId.Should().Be(contactUserId);
        value.Source.Should().Be("social-graph-contact-events");
    }

    [Fact]
    public void MapValue_WhenContactUserIdIsEmpty_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValue(CreateProjectionEvent(Guid.NewGuid(), Guid.Empty));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValue_WhenOwnerUserIdIsEmpty_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValue(CreateProjectionEvent(Guid.Empty, Guid.NewGuid()));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void GetDeduplicationKey_ReturnsObservedAndObserverUserIdTuple()
    {
        var ownerUserId = Guid.NewGuid();
        var contactUserId = Guid.NewGuid();
        var value = _factory.MapValue(CreateProjectionEvent(ownerUserId, contactUserId));

        var key = _factory.GetDeduplicationKey(value);

        key.Should().Be((contactUserId, ownerUserId));
    }

    private static ProjectionIntegrationEvent<ContactReadModel> CreateProjectionEvent(
        Guid ownerUserId,
        Guid contactUserId) =>
        new()
        {
            SourceAggregateId = Guid.NewGuid(),
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = OperationType.Updated,
            SourceAggregateVersion = 1,
            Value = new ContactReadModel
            {
                ContactId = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                ContactUserId = contactUserId,
                DisplayName = "Display Name",
                FirstName = "First",
                LastName = "Last",
                PhoneNumber = "+48123123123",
                EmailAddress = "contact@example.com",
                IsBlocked = false
            }
        };
}
