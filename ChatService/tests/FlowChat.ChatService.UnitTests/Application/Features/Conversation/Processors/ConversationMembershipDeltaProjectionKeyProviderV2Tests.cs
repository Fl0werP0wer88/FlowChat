using FlowChat.ChatService.Application.Features.Conversation.Processors;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Processors;

public sealed class ConversationMembershipDeltaProjectionKeyProviderV2Tests
{
    private readonly ConversationMembershipDeltaProjectionKeyProviderV2<object> _provider = new();

    [Fact]
    public void GetKafkaKey_WhenAllParticipantsBelongToConversation_ReturnsConversationId()
    {
        var conversationId = Id<ConversationV2>.New();
        var mutations = new[]
        {
            CreateMutation(conversationId),
            CreateMutation(conversationId)
        };

        var key = _provider.GetKafkaKey(new object(), mutations);

        key.Should().Be(conversationId.Value.ToString("D"));
    }

    [Fact]
    public void GetKafkaKey_WhenParticipantsBelongToDifferentConversations_Throws()
    {
        var mutations = new[]
        {
            CreateMutation(Id<ConversationV2>.New()),
            CreateMutation(Id<ConversationV2>.New())
        };

        var action = () => _provider.GetKafkaKey(new object(), mutations);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*same conversation*");
    }

    [Fact]
    public void GetKafkaKey_WhenMutationsAreEmpty_Throws()
    {
        var action = () => _provider.GetKafkaKey(
            new object(),
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
}
