using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.IntegrationTests.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private DbContextOptions<AppDbContext> _options = null!;
    private Guid _conversationId;
    private Guid _firstMessageId;
    private Guid _secondMessageId;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        await using var context = new AppDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        var creatorId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversation = GroupConversation.Create(
            Id<Conversation>.New(),
            creatorId,
            [creatorId, recipientId],
            "Concurrent sequencing test");
        MarkCreated(conversation);
        _conversationId = conversation.Id.Value;

        var firstMessage = CreateMessage(conversation.Id, creatorId, recipientId, "First");
        var secondMessage = CreateMessage(conversation.Id, creatorId, recipientId, "Second");
        _firstMessageId = firstMessage.Id.Value;
        _secondMessageId = secondMessage.Id.Value;

        context.Conversations.Add(conversation);
        context.ChatMessages.AddRange(firstMessage, secondMessage);
        await new ConversationMessageSequenceRepository(context).AddAsync(_conversationId);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task Handle_WhenTwoMessagesAreSequencedConcurrently_AssignsDistinctConsecutiveNumbers()
    {
        var results = await Task.WhenAll(
            AllocateSequenceAsync(_firstMessageId),
            AllocateSequenceAsync(_secondMessageId));

        results.Should().BeEquivalentTo([1L, 2L]);

        await using var verificationContext = new AppDbContext(_options);
        var persistedSequences = await verificationContext.ChatMessages
            .Where(message => message.Id == Id<ChatMessageAggregate>.FromGuid(_firstMessageId)
                              || message.Id == Id<ChatMessageAggregate>.FromGuid(_secondMessageId))
            .Select(message => message.SequenceNum)
            .ToListAsync();
        persistedSequences.Should().BeEquivalentTo([1L, 2L]);
    }

    private async Task<long> AllocateSequenceAsync(Guid messageId)
    {
        await using var context = new AppDbContext(_options);
        var dispatcher = new Mock<ILocalEventDispatcher>();
        dispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new SetChatMessageSequenceNumberCommandHandler(
            new ChatMessageWriteRepository(context),
            new ConversationMessageSequenceRepository(context),
            new EfUnitOfWork<AppDbContext>(context),
            dispatcher.Object,
            []);

        var result = await handler.Handle(
            new SetChatMessageSequenceNumberCommand(messageId, _conversationId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static ChatMessageAggregate CreateMessage(
        Id<Conversation> conversationId,
        Guid senderId,
        Guid recipientId,
        string text)
    {
        var message = ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.New(),
            conversationId,
            senderId,
            text,
            [recipientId]);
        MarkCreated(message);
        return message;
    }

    private static void MarkCreated(IAggregateRoot aggregate)
    {
        aggregate.SetCreated("integration-test");
        aggregate.SetUpdated("integration-test");
    }
}
