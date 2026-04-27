using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ConversationRepositoryTests
{
    [Fact]
    public async Task GroupConversationWriteRepository_GetByIdAsync_WhenGroupExists_ReturnsGroupWithParticipants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var createdByUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var groupConversation = GroupConversation.Create(createdByUserId, [createdByUserId, memberUserId], "Friends");
        var duetConversation = DuetConversation.Create(createdByUserId, Guid.NewGuid());

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.AddRange(groupConversation, duetConversation);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new GroupConversationWriteRepository(readContext);

        var result = await repository.GetByIdAsync(groupConversation.Id.Value, CancellationToken.None);
        var duetResult = await repository.GetByIdAsync(duetConversation.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(groupConversation.Id);
        result.Participants.Select(participant => participant.UserId)
            .Should().BeEquivalentTo([createdByUserId, memberUserId]);
        duetResult.Should().BeNull();
    }

    [Fact]
    public async Task DuetConversationWriteRepository_AddAsync_PersistsConversationAndDuetMapping()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateDbContext(connection);
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);
        var repository = new DuetConversationWriteRepository(context);

        await repository.AddAsync(conversation, CancellationToken.None);
        await context.SaveChangesAsync();

        var persistedConversation = await context.Set<DuetConversation>()
            .Include(x => x.Participants)
            .SingleAsync(x => x.Id == conversation.Id);
        var persistedMapping = await context.DuetConversations.SingleAsync();

        persistedConversation.Participants.Select(participant => participant.UserId)
            .Should().BeEquivalentTo([requestingUserId, partnerUserId]);
        persistedMapping.ConversationId.Should().Be(conversation.Id);
    }

    [Fact]
    public async Task ConversationParticipantReadRepository_GetParticipantUserIdsAsync_ReturnsParticipantsForAnyConversationType()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new ConversationParticipantReadRepository(readContext);

        var result = await repository.GetParticipantUserIdsAsync(conversation.Id.Value, CancellationToken.None);
        var missingResult = await repository.GetParticipantUserIdsAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeEquivalentTo([requestingUserId, partnerUserId]);
        missingResult.Should().BeNull();
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
