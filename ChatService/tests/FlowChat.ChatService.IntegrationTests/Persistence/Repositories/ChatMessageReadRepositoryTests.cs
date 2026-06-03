using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
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
        int hour) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderId,
            SenderDisplayName = senderId.ToString(),
            Text = text,
            SentAtUtc = new DateTimeOffset(2026, 4, 24, hour, 0, 0, TimeSpan.Zero)
        };

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
