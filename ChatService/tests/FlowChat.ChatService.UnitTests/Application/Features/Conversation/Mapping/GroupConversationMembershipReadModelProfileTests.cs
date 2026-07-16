using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Mapping;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Mapping;

public sealed class GroupConversationMembershipReadModelProfileTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GroupConversationMembershipReadModelProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Map_WhenGroupConversationProvided_MapsEveryParticipantMembership()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();
        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId],
            "Dev Team");

        var memberships = Mapper.Map<IEnumerable<ParticipantUser>>(conversation)
            .Select(Mapper.Map<GroupConversationMembershipReadModel>)
            .ToArray();

        memberships.Should().HaveCount(2);
        memberships.Should().OnlyContain(membership => membership.ConversationId == conversation.Id.Value);
        memberships.Select(membership => membership.ParticipantUserId)
            .Should().BeEquivalentTo([creatorId.Value, memberId.Value]);
    }
}
