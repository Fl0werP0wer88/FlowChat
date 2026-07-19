using AutoMapper;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Eventing.DomainEvents.ChatMessageSent;

public sealed class PublishChatMessageSentIntegrationEventDomainEventHandlerV2Tests
{
    [Fact]
    public async Task Handle_WhenMembershipExists_PublishesV2EventWithCurrentRevision()
    {
        var membershipRepository = new Mock<IConversationMembershipWriteRepository>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var conversationId = Id<ConversationV2>.New();
        var domainEvent = new ChatMessageSentDomainEventV2(
            Id<ChatMessageV2>.New(),
            conversationId,
            Id<UserProfile>.New(),
            "Hello",
            UtcDateTimeOffset.UtcNow);
        membershipRepository.Setup(x => x.GetVersionAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        IntegrationEventEnvelope<ChatMessageSentIntegrationEventV2>? published = null;
        publisher.Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ChatMessageSentIntegrationEventV2>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<ChatMessageSentIntegrationEventV2>, CancellationToken>(
                (envelope, _) => published = envelope)
            .Returns(Task.CompletedTask);
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<ChatMessageSentDomainEventV2Profile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var handler = new PublishChatMessageSentIntegrationEventDomainEventHandlerV2(
            membershipRepository.Object,
            publisher.Object,
            mapper);

        await handler.Handle(domainEvent, CancellationToken.None);

        published.Should().NotBeNull();
        published!.Payload.ConversationMembershipRevision.Should().Be(4);
        published.Payload.ConversationId.Should().Be(conversationId.Value);
    }

    [Fact]
    public async Task Handle_WhenMembershipDoesNotExist_ThrowsAndDoesNotPublish()
    {
        var membershipRepository = new Mock<IConversationMembershipWriteRepository>();
        var publisher = new Mock<IOutboxIntegrationEventPublisher>();
        var domainEvent = new ChatMessageSentDomainEventV2(
            Id<ChatMessageV2>.New(),
            Id<ConversationV2>.New(),
            Id<UserProfile>.New(),
            "Hello",
            UtcDateTimeOffset.UtcNow);
        membershipRepository.Setup(x => x.GetVersionAsync(
                It.IsAny<Id<ConversationV2>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);
        var mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<ChatMessageSentDomainEventV2Profile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var handler = new PublishChatMessageSentIntegrationEventDomainEventHandlerV2(
            membershipRepository.Object,
            publisher.Object,
            mapper);

        var action = () => handler.Handle(domainEvent, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        publisher.Verify(
            x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<ChatMessageSentIntegrationEventV2>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
