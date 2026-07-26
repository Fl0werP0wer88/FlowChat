using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class UnhideConversationParticipantsOnChatMessageSentDomainEventHandlerV2Tests
{
    [Fact]
    public async Task Handle_WhenRecipientIsHidden_UnhidesAndProcessesTrackedAggregate()
    {
        var conversationId = Id<ConversationV2>.New();
        var senderId = Id<UserProfile>.New();
        var hiddenParticipant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            ConversationType.Group,
            Id<UserProfile>.New(),
            duetPartnerUserId: null);
        hiddenParticipant.Hide();
        var repository = new Mock<IConversationParticipantWriteRepository>();
        repository.Setup(x => x.GetHiddenByConversationIdAsync(
                conversationId,
                senderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([hiddenParticipant]);
        var processor = new Mock<IAggregateBeforeSaveProcessorV2<
            ChatMessageSentDomainEventV2,
            ConversationParticipant>>();
        processor.Setup(x => x.ProcessAsync(
                It.IsAny<ChatMessageSentDomainEventV2>(),
                hiddenParticipant,
                MutationType.Updated,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new UnhideConversationParticipantsOnChatMessageSentDomainEventHandlerV2(
            repository.Object,
            [processor.Object]);
        var domainEvent = new ChatMessageSentDomainEventV2(
            Id<ChatMessageV2>.New(),
            conversationId,
            senderId,
            "Hello",
            UtcDateTimeOffset.UtcNow,
            sequenceNum: 42);

        await handler.Handle(domainEvent, CancellationToken.None);

        hiddenParticipant.IsHidden.Should().BeFalse();
        hiddenParticipant.Version.Should().Be(2);
        processor.Verify(x => x.ProcessAsync(
            domainEvent,
            hiddenParticipant,
            MutationType.Updated,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
