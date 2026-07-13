using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence;

public sealed class ChatMessageConfigurationTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenConversationDoesNotExist_ThrowsDbUpdateException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateDbContext(connection);
        var chatMessage = ChatMessage.Create(
            Id<ChatMessage>.New(),
            Id<Conversation>.New(),
            Guid.NewGuid(),
            "Hello",
            [Guid.NewGuid()]);

        context.ChatMessages.Add(chatMessage);

        var saveChanges = async () => await context.SaveChangesAsync();

        await saveChanges.Should().ThrowAsync<DbUpdateException>();
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
