using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Mapping;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Mapping;

public sealed class DuetConversationContactStateReadModelProfileTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<DuetConversationContactStateReadModelProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Map_WhenNeitherParticipantHasBlocked_MapsBothBlockedFlagsFalse()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var conversation = DuetConversationAggregate.Create(firstUserId, secondUserId);

        var readModel = Mapper.Map<DuetConversationContactStateReadModel>(conversation);

        readModel.ConversationId.Should().Be(conversation.Id.Value);
        readModel.FirstUserId.Should().Be(firstUserId);
        readModel.SecondUserId.Should().Be(secondUserId);
        readModel.FirstUserBlockedSecondUser.Should().BeFalse();
        readModel.SecondUserBlockedFirstUser.Should().BeFalse();
    }

    [Fact]
    public void Map_WhenFirstUserBlockedSecondUser_MapsOnlyThatDirectionTrue()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var conversation = DuetConversationAggregate.Create(firstUserId, secondUserId);
        conversation.BlockParticipant(firstUserId);

        var readModel = Mapper.Map<DuetConversationContactStateReadModel>(conversation);

        readModel.FirstUserBlockedSecondUser.Should().BeTrue();
        readModel.SecondUserBlockedFirstUser.Should().BeFalse();
    }

    [Fact]
    public void Map_WhenSecondUserBlockedFirstUser_MapsOnlyThatDirectionTrue()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var conversation = DuetConversationAggregate.Create(firstUserId, secondUserId);
        conversation.BlockParticipant(secondUserId);

        var readModel = Mapper.Map<DuetConversationContactStateReadModel>(conversation);

        readModel.FirstUserBlockedSecondUser.Should().BeFalse();
        readModel.SecondUserBlockedFirstUser.Should().BeTrue();
    }
}
