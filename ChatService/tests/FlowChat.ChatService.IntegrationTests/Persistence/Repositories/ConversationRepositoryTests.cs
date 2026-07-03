using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.Shared.Domain;
using FlowChat.ChatService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ConversationRepositoryTests
{
    [Fact]
    public async Task ConversationWriteRepository_GetByIdAsync_WhenConversationExists_ReturnsConversationForAnyConversationType()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var createdByUserId = Guid.NewGuid();
        var groupConversation = GroupConversation.Create(
            Id<Conversation>.New(),
            createdByUserId,
            [createdByUserId, Guid.NewGuid()],
            "Friends");
        var duetConversation = DuetConversation.Create(createdByUserId, Guid.NewGuid());
        MarkCreated(groupConversation);
        MarkCreated(duetConversation);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.AddRange(groupConversation, duetConversation);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new ConversationWriteRepository(readContext);

        var groupResult = await repository.GetByIdAsync(groupConversation.Id, CancellationToken.None);
        var duetResult = await repository.GetByIdAsync(duetConversation.Id, CancellationToken.None);

        groupResult.Should().BeOfType<GroupConversation>();
        duetResult.Should().BeOfType<DuetConversation>();
    }

    [Fact]
    public async Task GroupConversationWriteRepository_GetByIdAsync_WhenGroupExists_ReturnsGroupWithParticipants()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var createdByUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var groupConversation = GroupConversation.Create(Id<Conversation>.New(), createdByUserId, [createdByUserId, memberUserId], "Friends");
        var duetConversation = DuetConversation.Create(createdByUserId, Guid.NewGuid());
        MarkCreated(groupConversation);
        MarkCreated(duetConversation);

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
        result.Participants.Select(participant => participant.UserId.Value)
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
        MarkCreated(conversation);
        var repository = new DuetConversationWriteRepository(context);

        await repository.AddAsync(conversation, CancellationToken.None);
        await context.SaveChangesAsync();

        var persistedConversation = await context.Set<DuetConversation>()
            .Include(x => x.Participants)
            .SingleAsync(x => x.Id == conversation.Id);
        var persistedMapping = await context.DuetConversations.SingleAsync();

        persistedConversation.Participants.Select(participant => participant.UserId.Value)
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
        MarkCreated(conversation);

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
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void MarkCreated(Conversation conversation)
    {
        conversation.SetCreated("integration-test");
        conversation.SetUpdated("integration-test");
    }
}
