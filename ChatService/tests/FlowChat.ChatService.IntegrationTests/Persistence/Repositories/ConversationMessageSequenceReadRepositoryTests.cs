using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ConversationMessageSequenceReadRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();

    [Fact]
    [Trait("Category", "Docker")]
    public async Task GetCurrentAsync_WhenSequenceExists_ReturnsLastAssignedSequenceNumber()
    {
        await using var dbContext = CreateDbContext();
        var conversationId = await SeedConversationSequenceAsync(dbContext, 42);
        var repository = new ConversationMessageSequenceReadRepository(dbContext);

        var result = await repository.GetCurrentAsync(conversationId, CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task GetCurrentAsync_WhenSequenceDoesNotExist_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ConversationMessageSequenceReadRepository(dbContext);

        var result = await repository.GetCurrentAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Guid> SeedConversationSequenceAsync(
        AppDbContext dbContext,
        long lastAssignedSequenceNum)
    {
        var conversationId = Id<ConversationV2>.New();
        var conversation = ConversationV2.Restore(
            conversationId,
            ConversationType.Group,
            "Sequence read test",
            duetParticipants: null);
        conversation.SetCreated("test");
        conversation.SetUpdated("test");

        dbContext.ConversationsV2.Add(conversation);
        dbContext.ConversationMessageSequencesV2.Add(
            new ConversationMessageSequenceEntityV2
            {
                ConversationId = conversationId
            });
        await dbContext.SaveChangesAsync();
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "ConversationMessageSequencesV2"
             SET "LastAssignedSequenceNum" = {lastAssignedSequenceNum}
             WHERE "ConversationId" = {conversationId.Value}
             """);
        dbContext.ChangeTracker.Clear();
        return conversationId.Value;
    }
}
