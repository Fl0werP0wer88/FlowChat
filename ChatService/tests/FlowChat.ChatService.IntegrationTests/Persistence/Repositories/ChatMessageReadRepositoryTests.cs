using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ChatMessageReadRepositoryTests
{
    [Fact]
    public async Task GetRangeDescendingAsync_ReturnsNewestActiveRowsWithinInclusiveRange()
    {
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var deleted = Message(conversationId, senderId, 11);
        deleted.DeletedAt = DateTimeOffset.UtcNow;
        var databaseName = Guid.NewGuid().ToString();
        await Seed(databaseName,
            Message(conversationId, senderId, 8),
            Message(conversationId, senderId, 9),
            Message(conversationId, senderId, 10),
            deleted,
            Message(conversationId, senderId, 12));

        await using var context = Context(databaseName);
        var result = await new ChatMessageReadRepository(context)
            .GetRangeDescendingAsync(
                conversationId, startSequenceNum: 8,
                endSequenceNum: 10, limit: 3);

        result.Select(item => item.SequenceNum).Should().Equal(10, 9, 8);
    }

    [Fact]
    public async Task GetRangeDescendingAsync_ExcludesRowsOutsideRange()
    {
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var databaseName = Guid.NewGuid().ToString();
        await Seed(databaseName,
            Message(conversationId, senderId, 8),
            Message(conversationId, senderId, 9),
            Message(conversationId, senderId, 10));

        await using var context = Context(databaseName);
        var result = await new ChatMessageReadRepository(context)
            .GetRangeDescendingAsync(
                conversationId, startSequenceNum: 8,
                endSequenceNum: 9, limit: 10);

        result.Select(item => item.SequenceNum).Should().Equal(9, 8);
    }

    [Fact]
    public async Task GetRangeAscendingAsync_ReturnsActiveRowsWithinInclusiveRange()
    {
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var deleted = Message(conversationId, senderId, 11);
        deleted.DeletedAt = DateTimeOffset.UtcNow;
        var databaseName = Guid.NewGuid().ToString();
        await Seed(databaseName,
            Message(conversationId, senderId, 10),
            deleted,
            Message(conversationId, senderId, 12),
            Message(conversationId, senderId, 13));

        await using var context = Context(databaseName);
        var result = await new ChatMessageReadRepository(context)
            .GetRangeAscendingAsync(
                conversationId, startSequenceNum: 10,
                endSequenceNum: 12, limit: 100);

        result.Select(item => item.SequenceNum).Should().Equal(10, 12);
    }

    private static ChatMessageReadEntityV2 Message(
        Guid conversationId,
        Guid senderId,
        long sequenceNum) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderId,
            Text = $"Message {sequenceNum}",
            SentAtUtc = DateTimeOffset.UtcNow.AddSeconds(sequenceNum),
            SequenceNum = sequenceNum
        };

    private static async Task Seed(
        string databaseName,
        params ChatMessageReadEntityV2[] messages)
    {
        await using var context = Context(databaseName);
        context.ChatMessageReadsV2.AddRange(messages);
        await context.SaveChangesAsync();
    }

    private static AppDbContext Context(string databaseName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);
}
