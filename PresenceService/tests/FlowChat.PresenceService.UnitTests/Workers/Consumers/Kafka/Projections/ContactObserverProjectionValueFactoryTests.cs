using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.PresenceService.Consumers.Kafka.Projections;
using FluentAssertions;

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionValueFactoryTests
{
    private readonly ContactObserverProjectionValueFactory _factory = new();

    [Fact]
    public void MapValues_WhenNeitherUserHasBlocked_ReturnsBothDirectionsUnblocked()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        var values = _factory.MapValues(CreateProjectionEvent(firstUserId, secondUserId)).ToList();

        values.Should().HaveCount(2);
        values.Should().ContainSingle(v => v.ObserverUserId == firstUserId && v.ObservedUserId == secondUserId && !v.IsBlocked);
        values.Should().ContainSingle(v => v.ObserverUserId == secondUserId && v.ObservedUserId == firstUserId && !v.IsBlocked);
        values.Should().OnlyContain(v => v.Source == "chat-duet-conversation-events");
    }

    [Fact]
    public void MapValues_WhenFirstUserBlockedSecondUser_MarksOnlyThatDirectionBlocked()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        var values = _factory.MapValues(CreateProjectionEvent(
            firstUserId,
            secondUserId,
            firstUserBlockedSecondUser: true)).ToList();

        values.Should().ContainSingle(v => v.ObserverUserId == firstUserId && v.ObservedUserId == secondUserId && v.IsBlocked);
        values.Should().ContainSingle(v => v.ObserverUserId == secondUserId && v.ObservedUserId == firstUserId && !v.IsBlocked);
    }

    [Fact]
    public void MapValue_WhenFirstUserIdIsEmpty_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValue(CreateProjectionEvent(Guid.Empty, Guid.NewGuid()));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void MapValue_WhenSecondUserIdIsEmpty_ThrowsNonTransientException()
    {
        var act = () => _factory.MapValue(CreateProjectionEvent(Guid.NewGuid(), Guid.Empty));

        act.Should().Throw<NonTransientException>();
    }

    [Fact]
    public void GetDeduplicationKey_ReturnsObservedAndObserverUserIdTuple()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var value = _factory.MapValue(CreateProjectionEvent(firstUserId, secondUserId));

        var key = _factory.GetDeduplicationKey(value);

        key.Should().Be((secondUserId, firstUserId));
    }

    private static ProjectionIntegrationEvent<DuetConversationReadModel> CreateProjectionEvent(
        Guid firstUserId,
        Guid secondUserId,
        bool firstUserBlockedSecondUser = false,
        bool secondUserBlockedFirstUser = false) =>
        new()
        {
            SourceAggregateId = Guid.NewGuid(),
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = OperationType.Updated,
            SourceAggregateVersion = 1,
            Value = new DuetConversationReadModel
            {
                ConversationId = Guid.NewGuid(),
                FirstUserId = firstUserId,
                SecondUserId = secondUserId,
                FirstUserBlockedSecondUser = firstUserBlockedSecondUser,
                SecondUserBlockedFirstUser = secondUserBlockedFirstUser
            }
        };
}
