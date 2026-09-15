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

public sealed class ConversationMessageSequenceRepositoryV2Tests : IAsyncLifetime
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
    public async Task GetNextAsync_WithoutTransaction_Throws()
    {
        await using var dbContext = CreateDbContext();
        var conversationId = await SeedConversationAsync(dbContext);
        var repository = new ConversationMessageSequenceRepositoryV2(dbContext);

        var action = () => repository.GetNextAsync(conversationId, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*transaction is required*");
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task GetNextAsync_WhenCalledSequentially_ReturnsIncreasingNumbers()
    {
        await using var dbContext = CreateDbContext();
        var conversationId = await SeedConversationAsync(dbContext);
        var repository = new ConversationMessageSequenceRepositoryV2(dbContext);

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var first = await repository.GetNextAsync(conversationId, CancellationToken.None);
        var second = await repository.GetNextAsync(conversationId, CancellationToken.None);
        await transaction.CommitAsync();

        first.Should().Be(1);
        second.Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task GetNextAsync_WhenTransactionRollsBack_DoesNotConsumeNumber()
    {
        Id<ConversationV2> conversationId;
        await using (var seedContext = CreateDbContext())
        {
            conversationId = await SeedConversationAsync(seedContext);
        }

        await using (var rollbackContext = CreateDbContext())
        {
            await using var transaction = await rollbackContext.Database.BeginTransactionAsync();
            var repository = new ConversationMessageSequenceRepositoryV2(rollbackContext);
            var rolledBackNumber = await repository.GetNextAsync(conversationId, CancellationToken.None);
            rolledBackNumber.Should().Be(1);
            await transaction.RollbackAsync();
        }

        await using var verificationContext = CreateDbContext();
        await using var verificationTransaction = await verificationContext.Database.BeginTransactionAsync();
        var allocatedNumber = await new ConversationMessageSequenceRepositoryV2(verificationContext)
            .GetNextAsync(conversationId, CancellationToken.None);
        await verificationTransaction.CommitAsync();

        allocatedNumber.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task GetNextAsync_WhenCalledConcurrently_SerializesAllocations()
    {
        Id<ConversationV2> conversationId;
        await using (var seedContext = CreateDbContext())
        {
            conversationId = await SeedConversationAsync(seedContext);
        }

        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        await using var firstTransaction = await firstContext.Database.BeginTransactionAsync();
        await using var secondTransaction = await secondContext.Database.BeginTransactionAsync();
        var firstRepository = new ConversationMessageSequenceRepositoryV2(firstContext);
        var secondRepository = new ConversationMessageSequenceRepositoryV2(secondContext);

        var first = await firstRepository.GetNextAsync(conversationId, CancellationToken.None);
        var secondTask = secondRepository.GetNextAsync(conversationId, CancellationToken.None);
        await firstTransaction.CommitAsync();
        var second = await secondTask;
        await secondTransaction.CommitAsync();

        first.Should().Be(1);
        second.Should().Be(2);
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<Id<ConversationV2>> SeedConversationAsync(AppDbContext dbContext)
    {
        var conversationId = Id<ConversationV2>.New();
        var conversation = ConversationV2.Restore(
            conversationId,
            ConversationType.Group,
            "Sequence test",
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
        dbContext.ChangeTracker.Clear();
        return conversationId;
    }
}
