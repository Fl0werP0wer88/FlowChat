using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ChatMessageReadRepositoryTests
{
    [Fact]
    public async Task GetMaxSequenceNumAsync_ReturnsHighestActiveSequenceForConversation()
    {
        var conversationId = Guid.NewGuid();
        var otherConversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var deleted = CreateMessage(conversationId, senderId, "Deleted", 11, 99);
        deleted.DeletedAt = new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero);
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.ChatMessageReads.AddRange(
                CreateMessage(conversationId, senderId, "Unsequenced", 8),
                CreateMessage(conversationId, senderId, "First", 9, 4),
                CreateMessage(conversationId, senderId, "Latest", 10, 7),
                CreateMessage(otherConversationId, senderId, "Other", 10, 42),
                deleted);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(databaseName);
        var result = await new ChatMessageReadRepository(readContext)
            .GetMaxSequenceNumAsync(conversationId, CancellationToken.None);

        result.Should().Be(7);
    }

    [Fact]
    public async Task GetMaxSequenceNumAsync_WhenConversationHasNoSequencedMessages_ReturnsNull()
    {
        var conversationId = Guid.NewGuid();
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.ChatMessageReads.Add(
                CreateMessage(conversationId, Guid.NewGuid(), "Unsequenced", 8));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(databaseName);
        var result = await new ChatMessageReadRepository(readContext)
            .GetMaxSequenceNumAsync(conversationId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPageBeforeAsync_WithoutCursor_ReturnsNewestMessagesAndNextCursor()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var oldest = CreateMessage(conversationId, senderId, "Oldest", 8);
        var middle = CreateMessage(conversationId, recipientId, "Middle", 9);
        var newest = CreateMessage(conversationId, senderId, "Newest", 10);
        var deleted = CreateMessage(conversationId, senderId, "Deleted", 11);
        deleted.DeletedAt = new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero);
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.ChatMessageReads.AddRange(oldest, middle, newest, deleted);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(databaseName);
        var repository = new ChatMessageReadRepository(readContext);

        var result = await repository.GetPageBeforeAsync(conversationId, 2, null, null, CancellationToken.None);

        result.Items.Select(message => message.Text).Should().Equal("Newest", "Middle");
        result.HasMore.Should().BeTrue();
        result.NextBeforeSentAtUtc.Should().Be(middle.SentAtUtc);
        result.NextBeforeMessageId.Should().Be(middle.Id);
    }

    [Fact]
    public async Task GetPageBeforeAsync_WithCursor_ReturnsOlderMessagesWithoutDuplicates()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var oldest = CreateMessage(conversationId, senderId, "Oldest", 8);
        var middle = CreateMessage(conversationId, recipientId, "Middle", 9);
        var newest = CreateMessage(conversationId, senderId, "Newest", 10);
        var databaseName = Guid.NewGuid().ToString();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.ChatMessageReads.AddRange(oldest, middle, newest);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(databaseName);
        var repository = new ChatMessageReadRepository(readContext);

        var firstPage = await repository.GetPageBeforeAsync(conversationId, 2, null, null, CancellationToken.None);
        var secondPage = await repository.GetPageBeforeAsync(
            conversationId,
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

    private static ChatMessageReadEntity CreateMessage(
        Guid conversationId,
        Guid senderId,
        string text,
        int hour,
        long? sequenceNum = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderId,
            Text = text,
            SentAtUtc = new DateTimeOffset(2026, 4, 24, hour, 0, 0, TimeSpan.Zero),
            SequenceNum = sequenceNum
        };

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        return context;
    }
}
