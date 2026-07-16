using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Migrations;

public sealed class ConversationMessageSequenceMigrationTests : IAsyncLifetime
{
    private const string InitialMigration = "20260716085618_InitialCreate";

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private DbContextOptions<AppDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
    }

    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task MigrateAsync_WhenConversationsAlreadyExist_BackfillsLastAssignedSequenceNumber()
    {
        var creatorId = Guid.NewGuid();
        var conversation = GroupConversation.Create(
            Id<Conversation>.New(),
            creatorId,
            [creatorId, Guid.NewGuid()],
            "Migration test");
        conversation.SetCreated("integration-test");
        conversation.SetUpdated("integration-test");

        var message = ChatMessage.Create(
            Id<ChatMessage>.New(),
            conversation.Id,
            creatorId,
            "Already sequenced",
            [Guid.NewGuid()]);
        message.SetSequenceNumber(7);
        message.SetCreated("integration-test");
        message.SetUpdated("integration-test");

        await using (var seedContext = new AppDbContext(_options))
        {
            await seedContext.GetService<IMigrator>().MigrateAsync(InitialMigration);
            await seedContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Conversations\" ALTER COLUMN \"LastMsgSequenceNum\" SET DEFAULT 0;");
            seedContext.Conversations.Add(conversation);
            seedContext.ChatMessages.Add(message);
            await seedContext.SaveChangesAsync();
            await seedContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Conversations\" ALTER COLUMN \"LastMsgSequenceNum\" DROP DEFAULT;");
        }

        await using (var migrationContext = new AppDbContext(_options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using var verificationContext = new AppDbContext(_options);
        var sequence = await verificationContext.ConversationMessageSequences.SingleAsync();
        sequence.ConversationId.Value.Should().Be(conversation.Id.Value);
        sequence.LastAssignedSequenceNum.Should().Be(7);
    }
}
