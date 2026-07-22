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
    public void Map_ToMembershipReadModel_MapsConversationAndParticipantUserIds()
    {
        var participant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            Id<ConversationV2>.New(),
            Id<UserProfileMarker>.New());

        var readModel = Mapper.Map<ConversationMembershipReadModelV2>(participant);

        readModel.ConversationId.Should().Be(participant.ConversationId.Value);
        readModel.ParticipantUserId.Should().Be(participant.UserId.Value);
    }
}
