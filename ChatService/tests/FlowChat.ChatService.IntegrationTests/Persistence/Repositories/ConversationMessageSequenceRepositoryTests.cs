using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ConversationMessageSequenceRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private DbContextOptions<AppDbContext> _options = null!;
    private Guid _conversationId;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        await using var context = new AppDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        var creatorId = Guid.NewGuid();
        var conversation = GroupConversation.Create(
            Id<Conversation>.New(),
            creatorId,
            [creatorId, Guid.NewGuid()],
            "Sequence allocation test");
        conversation.SetCreated("integration-test");
        conversation.SetUpdated("integration-test");
        _conversationId = conversation.Id.Value;

        context.Conversations.Add(conversation);
        await new ConversationMessageSequenceRepository(context).AddAsync(_conversationId);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => _postgresContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task GetNextAsync_WhenCalledConcurrently_ReturnsDistinctConsecutiveSequenceNumbers()
    {
        var allocations = await Task.WhenAll(AllocateNextAsync(), AllocateNextAsync());

        allocations.Should().BeEquivalentTo([1L, 2L]);

        await using var verificationContext = new AppDbContext(_options);
        var persistedSequence = await verificationContext.ConversationMessageSequences.SingleAsync();
        persistedSequence.LastAssignedSequenceNum.Should().Be(2);
    }

    [Fact]
    public async Task GetNextAsync_WhenTransactionIsRolledBack_DoesNotConsumeSequenceNumber()
    {
        await using (var rollbackContext = new AppDbContext(_options))
        {
            await using var transaction = await rollbackContext.Database.BeginTransactionAsync();
            var allocated = await new ConversationMessageSequenceRepository(rollbackContext)
                .GetNextAsync(_conversationId);
            allocated.Should().Be(1);
            await transaction.RollbackAsync();
        }

        var allocatedAfterRollback = await AllocateNextAsync();

        allocatedAfterRollback.Should().Be(1);
    }

    private async Task<long> AllocateNextAsync()
    {
        await using var context = new AppDbContext(_options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var sequenceNum = await new ConversationMessageSequenceRepository(context)
            .GetNextAsync(_conversationId);
        await transaction.CommitAsync();
        return sequenceNum;
    }
}
