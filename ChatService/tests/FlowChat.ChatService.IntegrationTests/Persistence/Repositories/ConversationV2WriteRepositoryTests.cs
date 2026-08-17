using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ConversationV2WriteRepositoryTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task ConversationsV2_WhenDuetPairShapeDoesNotMatchConversationType_RejectsRow(
        int conversationType,
        bool includeDuetPair)
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = CreateDbContext(connection);
        string? name = conversationType == 2 ? "Group" : null;
        Guid? firstUserId = includeDuetPair
            ? Guid.Parse("00000000-0000-0000-0000-000000000001")
            : null;
        Guid? secondUserId = includeDuetPair
            ? Guid.Parse("00000000-0000-0000-0000-000000000002")
            : null;
        var now = DateTimeOffset.UtcNow;

        var act = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "ConversationsV2" (
                "Id", "ConversationType", "Name", "DuetFirstUserId", "DuetSecondUserId",
                "Version", "CreatedBy", "CreatedAtUtc", "LastModifiedBy", "LastModifiedAtUtc")
            VALUES (
                {Guid.NewGuid()}, {conversationType}, {name}, {firstUserId}, {secondUserId},
                {0}, {"integration-test"}, {now}, {"integration-test"}, {now})
            """);

        await act.Should().ThrowAsync<SqliteException>();
    }

    [Fact]
    public async Task GetDuetByUserIdsAsync_WhenPairIsPassedInEitherOrder_ReturnsConversation()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var firstUserId = Id<UserProfile>.New();
        var secondUserId = Id<UserProfile>.New();
        var conversation = CreateDuet(firstUserId, secondUserId);

        await using (var seedContext = CreateDbContext(connection))
        {
            await new ConversationV2WriteRepository(seedContext).AddAsync(conversation);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new ConversationV2WriteRepository(readContext);

        var forward = await repository.GetDuetByUserIdsAsync(firstUserId, secondUserId);
        var reverse = await repository.GetDuetByUserIdsAsync(secondUserId, firstUserId);

        forward.Should().NotBeNull();
        reverse.Should().NotBeNull();
        forward!.Id.Should().Be(conversation.Id);
        reverse!.Id.Should().Be(conversation.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenActiveDuetPairAlreadyExists_ThrowsDbUpdateException()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var firstUserId = Id<UserProfile>.New();
        var secondUserId = Id<UserProfile>.New();
        await using var context = CreateDbContext(connection);
        var repository = new ConversationV2WriteRepository(context);

        await repository.AddAsync(CreateDuet(firstUserId, secondUserId));
        await context.SaveChangesAsync();
        await repository.AddAsync(CreateDuet(secondUserId, firstUserId));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenPreviousDuetPairIsSoftDeleted_AllowsReplacement()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var firstUserId = Id<UserProfile>.New();
        var secondUserId = Id<UserProfile>.New();
        await using var context = CreateDbContext(connection);
        var repository = new ConversationV2WriteRepository(context);
        var previousConversation = CreateDuet(firstUserId, secondUserId);

        await repository.AddAsync(previousConversation);
        await context.SaveChangesAsync();
        previousConversation.Delete(UtcDateTimeOffset.UtcNow);
        await context.SaveChangesAsync();

        var replacement = CreateDuet(firstUserId, secondUserId);
        await repository.AddAsync(replacement);
        var act = () => context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
        (await repository.GetDuetByUserIdsAsync(firstUserId, secondUserId))!
            .Id.Should().Be(replacement.Id);
    }

    private static ConversationV2 CreateDuet(Id<UserProfile> firstUserId, Id<UserProfile> secondUserId)
    {
        var conversation = ConversationV2.CreateDuet(firstUserId, secondUserId);
        conversation.SetCreated("integration-test");
        conversation.SetUpdated("integration-test");
        return conversation;
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
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
