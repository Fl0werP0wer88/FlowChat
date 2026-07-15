using AutoMapper;
using FlowChat.ChatService.Application.Features.Conversation.Mapping;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using DuetConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Mapping;

public sealed class DuetConversationMembershipReadModelProfileTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<DuetConversationMembershipReadModelProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Map_WhenConversationCreated_MapsMembershipState()
    {
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var conversation = DuetConversationAggregate.Create(firstUserId, secondUserId);

        var readModel = Mapper.Map<DuetConversationMembershipReadModel>(conversation);

        readModel.ConversationId.Should().Be(conversation.Id.Value);
        readModel.FirstUserId.Should().Be(firstUserId);
        readModel.SecondUserId.Should().Be(secondUserId);
        readModel.ConversationMembershipRevision.Should().Be(conversation.MembershipRevision);
    }
}
