using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.PresenceService.Consumers.Kafka.Projections;
using FluentAssertions;

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionValueFactoryTests
{
    private const int DuetConversationType = 1;
    private const int GroupConversationType = 2;

    private readonly ContactObserverProjectionValueFactory _factory = new();

    [Fact]
    public void MapValue_DuetParticipant_ReturnsParticipantOwnedDirection()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();

        var value = _factory.MapValue(CreateProjectionEvent(
            userId: userId,
            duetPartnerUserId: partnerUserId,
            isBlocked: true));

        value.ObserverUserId.Should().Be(userId);
        value.ObservedUserId.Should().Be(partnerUserId);
        value.IsBlocked.Should().BeTrue();
        value.Source.Should().Be("chat-conversation-participant-v2");
    }

    [Fact]
    public void MapValues_TwoDuetParticipants_ReturnsIndependentDirections()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        var firstDirection = _factory.MapValues(CreateProjectionEvent(
            userId: firstUserId,
            duetPartnerUserId: secondUserId)).Single();
        var secondDirection = _factory.MapValues(CreateProjectionEvent(
            userId: secondUserId,
            duetPartnerUserId: firstUserId,
            isBlocked: true)).Single();

        firstDirection.ObserverUserId.Should().Be(firstUserId);
        firstDirection.ObservedUserId.Should().Be(secondUserId);
        firstDirection.IsBlocked.Should().BeFalse();
        secondDirection.ObserverUserId.Should().Be(secondUserId);
        secondDirection.ObservedUserId.Should().Be(firstUserId);
        secondDirection.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public void MapValues_GroupParticipant_ReturnsNoDirections()
    {
        var message = CreateProjectionEvent(
            conversationType: GroupConversationType,
            omitDuetPartner: true);

        var values = _factory.MapValues(message);

        values.Should().BeEmpty();
    }

    [Fact]
    public void MapValues_MissingValue_ThrowsNonTransientException()
    {
        var message = CreateProjectionEvent() with
        {
            Value = null!
        };

        var act = () => _factory.MapValues(message);

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_SourceAggregateIdDoesNotMatchParticipantId_ThrowsNonTransientException()
    {
        var message = CreateProjectionEvent() with
        {
            SourceAggregateId = Guid.NewGuid()
        };

        var act = () => _factory.MapValues(message);

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_EmptyParticipantId_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValues(CreateProjectionEvent(participantId: Guid.Empty));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_EmptyConversationId_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValues(CreateProjectionEvent(conversationId: Guid.Empty));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_EmptyUserId_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValues(CreateProjectionEvent(userId: Guid.Empty));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_UnknownConversationType_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValues(CreateProjectionEvent(conversationType: 999));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_DuetWithoutPartner_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValues(CreateProjectionEvent(omitDuetPartner: true));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_DuetWithEmptyPartner_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValues(CreateProjectionEvent(duetPartnerUserId: Guid.Empty));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_DuetWithSelfAsPartner_ThrowsNonTransientException()
    {
        var userId = Guid.NewGuid();

        var act = () => _factory.MapValues(CreateProjectionEvent(
            userId: userId,
            duetPartnerUserId: userId));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValues_GroupWithDuetPartner_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValues(CreateProjectionEvent(
            conversationType: GroupConversationType,
            duetPartnerUserId: Guid.NewGuid()));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void GetDeduplicationKey_ReturnsObservedAndObserverUserIdTuple()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var value = _factory.MapValue(CreateProjectionEvent(
            userId: userId,
            duetPartnerUserId: partnerUserId));

        var key = _factory.GetDeduplicationKey(value);

        key.Should().Be((partnerUserId, userId));
    }

    private static ProjectionIntegrationEvent<ConversationParticipantReadModelV2> CreateProjectionEvent(
        Guid? participantId = null,
        Guid? conversationId = null,
        int conversationType = DuetConversationType,
        Guid? userId = null,
        Guid? duetPartnerUserId = null,
        bool omitDuetPartner = false,
        bool isBlocked = false)
    {
        var resolvedParticipantId = participantId ?? Guid.NewGuid();

        return new ProjectionIntegrationEvent<ConversationParticipantReadModelV2>
        {
            SourceAggregateId = resolvedParticipantId,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = OperationType.Updated,
            SourceAggregateVersion = 2,
            Value = new ConversationParticipantReadModelV2
            {
                ParticipantId = resolvedParticipantId,
                ConversationId = conversationId ?? Guid.NewGuid(),
                ConversationType = conversationType,
                UserId = userId ?? Guid.NewGuid(),
                DuetPartnerUserId = omitDuetPartner ? null : duetPartnerUserId ?? Guid.NewGuid(),
                IsBlocked = isBlocked,
                IsMuted = false,
                IsHidden = false,
                JoinedAtUtc = DateTimeOffset.UtcNow,
                LastReadMessageSequenceNum = 0
            }
        };
    }
}
