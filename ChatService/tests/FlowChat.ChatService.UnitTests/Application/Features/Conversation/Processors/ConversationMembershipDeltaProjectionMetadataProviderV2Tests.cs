using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Processors;

public sealed class ConversationMembershipDeltaProjectionMetadataProviderV2Tests
{
    private readonly ConversationMembershipDeltaProjectionMetadataProviderV2<
        ConversationParticipantsAddedDomainEventV2> _provider = new();

    [Fact]
    public void GetKafkaKey_WhenAllParticipantsBelongToConversation_ReturnsConversationId()
    {
        var conversationId = Id<ConversationV2>.New();
        var mutations = new[]
        {
            CreateMutation(conversationId),
            CreateMutation(conversationId)
        };

        var key = _provider.GetKafkaKey(CreateNotification(conversationId), mutations);

        key.Should().Be(conversationId.Value.ToString("D"));
    }

    [Fact]
    public void GetKafkaKey_WhenParticipantBelongsToDifferentConversationThanNotification_Throws()
    {
        var notification = CreateNotification(Id<ConversationV2>.New());
        var mutations = new[]
        {
            CreateMutation(Id<ConversationV2>.New())
        };

        var action = () => _provider.GetKafkaKey(notification, mutations);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*conversation from the notification*");
    }

    [Fact]
    public void GetKafkaKey_WhenMutationsAreEmpty_Throws()
    {
        var action = () => _provider.GetKafkaKey(
            CreateNotification(Id<ConversationV2>.New()),
            Array.Empty<AggregateDeltaMutation<ConversationParticipant>>());

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one participant mutation*");
    }

    private static AggregateDeltaMutation<ConversationParticipant> CreateMutation(
        Id<ConversationV2> conversationId)
    {
        var participant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            Id<UserProfileMarker>.New());

        return new AggregateDeltaMutation<ConversationParticipant>(participant, MutationType.Created);
    }

    private static ConversationParticipantsAddedDomainEventV2 CreateNotification(
        Id<ConversationV2> conversationId) =>
        new(
            Id<ConversationMembership>.FromId(conversationId),
            conversationId,
            [Id<UserProfileMarker>.New()],
            initialReadCursor: 0);
}
