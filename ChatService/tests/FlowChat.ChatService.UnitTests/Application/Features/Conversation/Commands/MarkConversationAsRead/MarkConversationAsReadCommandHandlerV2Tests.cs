using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.MarkConversationAsRead;

public sealed class MarkConversationAsReadCommandHandlerV2Tests
{
    [Fact]
    public async Task Handle_WhenNewMessagesExist_AdvancesOnlyParticipantAggregate()
    {
        var conversationId = Id<ConversationV2>.New();
        var userId = Id<UserProfile>.New();
        var participant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            userId);
        var participantRepository = new Mock<IConversationParticipantWriteRepository>();
        participantRepository.Setup(x => x.GetActiveAsync(
                conversationId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        var messageRepository = new Mock<IChatMessageV2WriteRepository>();
        messageRepository.Setup(x => x.GetMaxSequenceNumAsync(
                conversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        var unitOfWork = CreateUnitOfWork();
        var dispatcher = new Mock<ILocalEventDispatcher>();
        dispatcher.Setup(x => x.DispatchAsync(
                It.IsAny<IEnumerable<ILocalEvent>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var processor = new Mock<IAggregateBeforeSaveProcessorV2<
            MarkConversationAsReadCommandV2,
            ConversationParticipant>>();
        processor.Setup(x => x.ProcessAsync(
                It.IsAny<MarkConversationAsReadCommandV2>(),
                participant,
                MutationType.Updated,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new MarkConversationAsReadCommandHandlerV2(
            participantRepository.Object,
            messageRepository.Object,
            unitOfWork.Object,
            dispatcher.Object,
            [processor.Object]);
        var command = new MarkConversationAsReadCommandV2(
            conversationId.Value,
            userId.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        participant.LastReadMessageSequenceNum.Should().Be(12);
        participant.Version.Should().Be(2);
        processor.Verify(x => x.ProcessAsync(
            command,
            participant,
            MutationType.Updated,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCursorIsCurrent_DoesNotIncrementVersionOrPublish()
    {
        var conversationId = Id<ConversationV2>.New();
        var userId = Id<UserProfile>.New();
        var participant = ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            userId,
            lastReadMessageSequenceNum: 12);
        var participantRepository = new Mock<IConversationParticipantWriteRepository>();
        participantRepository.Setup(x => x.GetActiveAsync(
                conversationId,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
        var messageRepository = new Mock<IChatMessageV2WriteRepository>();
        messageRepository.Setup(x => x.GetMaxSequenceNumAsync(
                conversationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        var processor = new Mock<IAggregateBeforeSaveProcessorV2<
            MarkConversationAsReadCommandV2,
            ConversationParticipant>>();
        var handler = new MarkConversationAsReadCommandHandlerV2(
            participantRepository.Object,
            messageRepository.Object,
            CreateUnitOfWork().Object,
            Mock.Of<ILocalEventDispatcher>(),
            [processor.Object]);

        var result = await handler.Handle(
            new MarkConversationAsReadCommandV2(conversationId.Value, userId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        participant.Version.Should().Be(1);
        processor.Verify(x => x.ProcessAsync(
            It.IsAny<MarkConversationAsReadCommandV2>(),
            It.IsAny<ConversationParticipant>(),
            It.IsAny<MutationType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));
        return unitOfWork;
    }
}
