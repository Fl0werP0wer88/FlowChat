using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ChatMessageReadRepositoryTests
{
    [Fact]
    public async Task GetPageBeforeAsync_WithoutCursor_ReturnsNewestMessagesAndNextCursor()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversation = CreateConversation(senderId, recipientId);
        var oldest = CreateMessage(conversation.Id, senderId, recipientId, "Oldest", 8);
        var middle = CreateMessage(conversation.Id, recipientId, senderId, "Middle", 9);
        var newest = CreateMessage(conversation.Id, senderId, recipientId, "Newest", 10);
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.ChatMessages.AddRange(oldest, middle, newest);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(databaseName);
        var repository = new ChatMessageReadRepository(readContext);

        var result = await repository.GetPageBeforeAsync(conversation.Id.Value, 2, null, null, CancellationToken.None);

        result.Items.Select(message => message.Text).Should().Equal("Newest", "Middle");
        result.HasMore.Should().BeTrue();
        result.NextBeforeSentAtUtc.Should().Be(middle.SentAtUtc.Value);
        result.NextBeforeMessageId.Should().Be(middle.Id.Value);
    }

    [Fact]
    public async Task GetPageBeforeAsync_WithCursor_ReturnsOlderMessagesWithoutDuplicates()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversation = CreateConversation(senderId, recipientId);
        var oldest = CreateMessage(conversation.Id, senderId, recipientId, "Oldest", 8);
        var middle = CreateMessage(conversation.Id, recipientId, senderId, "Middle", 9);
        var newest = CreateMessage(conversation.Id, senderId, recipientId, "Newest", 10);
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.ChatMessages.AddRange(oldest, middle, newest);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(databaseName);
        var repository = new ChatMessageReadRepository(readContext);

        var firstPage = await repository.GetPageBeforeAsync(conversation.Id.Value, 2, null, null, CancellationToken.None);
        var secondPage = await repository.GetPageBeforeAsync(
            conversation.Id.Value,
            2,
            firstPage.NextBeforeSentAtUtc,
            firstPage.NextBeforeMessageId,
            CancellationToken.None);

        secondPage.Items.Select(message => message.Text).Should().Equal("Oldest");
        secondPage.Items.Select(message => message.Id)
            .Should().NotIntersectWith(firstPage.Items.Select(message => message.Id));
        secondPage.HasMore.Should().BeFalse();
        secondPage.NextBeforeSentAtUtc.Should().BeNull();
        secondPage.NextBeforeMessageId.Should().BeNull();
    }

    private static Conversation CreateConversation(Guid senderId, Guid recipientId) =>
        FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation.Create(
            createdByUserId: senderId,
            partnerUserId: recipientId);

    private static ChatMessage CreateMessage(
        Id<Conversation> conversationId,
        Guid senderId,
        Guid recipientId,
        string text,
        int hour) =>
        ChatMessage.Create(
            Id<ChatMessage>.New(),
            conversationId,
            senderId,
            senderId.ToString(),
            text,
            [recipientId],
            UtcDateTimeOffset.Create(new DateTimeOffset(2026, 4, 24, hour, 0, 0, TimeSpan.Zero)));

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        var context = new AppDbContext(options);
        return context;
    }
}
