using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Mapping;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Mapping;

public sealed class ConversationParticipantReadModelV2ProfileTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<ConversationParticipantReadModelV2Profile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Map_ToReadModels_MapsConversationTypeAndIdentifiers()
    {
        var participant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            Id<ConversationV2>.New(),
            ConversationType.Duet,
            Id<UserProfileMarker>.New());

        var membershipReadModel = Mapper.Map<ConversationMembershipReadModelV2>(participant);
        var participantReadModel = Mapper.Map<ConversationParticipantReadModelV2>(participant);

        membershipReadModel.ConversationId.Should().Be(participant.ConversationId.Value);
        membershipReadModel.ParticipantUserId.Should().Be(participant.UserId.Value);
        participantReadModel.ParticipantId.Should().Be(participant.Id.Value);
        participantReadModel.ConversationId.Should().Be(participant.ConversationId.Value);
        participantReadModel.ConversationType.Should().Be((int)ConversationType.Duet);
        participantReadModel.UserId.Should().Be(participant.UserId.Value);
    }
}
