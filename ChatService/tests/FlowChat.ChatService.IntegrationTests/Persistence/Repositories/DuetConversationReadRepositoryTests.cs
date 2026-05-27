using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class DuetConversationReadRepositoryTests
{
    [Fact]
    public async Task GetByUserIdsAsync_WhenDuetConversationExists_ReturnsOrderedParticipantDetails()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation.Create(
            createdByUserId: requestingUserId,
            partnerUserId: partnerUserId);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.DuetConversations.Add(CreateDuetConversation(requestingUserId, partnerUserId, conversation.Id.Value));
            seedContext.UserProfileProjections.AddRange(
                CreateProfile(requestingUserId, "requester", "Requester", "requester.png"),
                CreateProfile(partnerUserId, "partner", "Partner", "partner.png"));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new DuetConversationReadRepository(readContext);

        var result = await repository.GetByUserIdsAsync(requestingUserId, partnerUserId, CancellationToken.None);
        var participants = result!.Participants.ToList();

        result.Should().NotBeNull();
        result.ConversationId.Should().Be(conversation.Id.Value);
        participants.Should().HaveCount(2);
        participants.Select(x => x.UserId).Should().Equal(requestingUserId, partnerUserId);
        participants.Select(x => x.ParticipantUserId).Should().Equal(requestingUserId, partnerUserId);
        participants.Select(x => x.DisplayName).Should().Equal("Requester", "Partner");
        participants.Select(x => x.AvatarUrl).Should().Equal("requester.png", "partner.png");
    }

    [Fact]
    public async Task GetByUserIdsAsync_WhenProfileIsMissing_ReturnsFallbackValuesForMissingProjection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation.Create(
            createdByUserId: requestingUserId,
            partnerUserId: partnerUserId);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.DuetConversations.Add(CreateDuetConversation(requestingUserId, partnerUserId, conversation.Id.Value));
            seedContext.UserProfileProjections.Add(CreateProfile(requestingUserId, "requester", "Requester", "requester.png"));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new DuetConversationReadRepository(readContext);

        var result = await repository.GetByUserIdsAsync(requestingUserId, partnerUserId, CancellationToken.None);
        var participants = result!.Participants.ToList();

        result.Should().NotBeNull();
        participants.Should().HaveCount(2);
        participants[1].UserId.Should().Be(partnerUserId);
        participants[1].DisplayName.Should().BeNull();
        participants[1].AvatarUrl.Should().BeNull();
        participants[1].ParticipantUserId.Should().Be(partnerUserId);
    }

    [Fact]
    public async Task GetByUserIdsAsync_WhenDuetMappingPointsToMissingConversation_ReturnsNull()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var missingConversationId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            await seedContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
            var normalizedIds = Normalize(requestingUserId, partnerUserId);
            await seedContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "DuetConversations" ("FirstUserId", "SecondUserId", "ConversationId")
                VALUES ({normalizedIds.First}, {normalizedIds.Second}, {missingConversationId})
                """);
            await seedContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new DuetConversationReadRepository(readContext);

        var result = await repository.GetByUserIdsAsync(requestingUserId, partnerUserId, CancellationToken.None);

        result.Should().BeNull();
    }

    private static DuetConversationLookupEntity CreateDuetConversation(Guid userId1, Guid userId2, Guid conversationId)
    {
        var (first, second) = Normalize(userId1, userId2);

        return new DuetConversationLookupEntity
        {
            FirstUserId = first,
            SecondUserId = second,
            ConversationId = Id<Conversation>.FromGuid(conversationId)
        };
    }

    private static UserProfileProjectionEntity CreateProfile(
        Guid userId,
        string friendlyUserId,
        string? displayName,
        string? avatarUrl) =>
        new()
        {
            UserId = userId,
            FriendlyUserId = friendlyUserId,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            CreatedBy = "test",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 21, 10, 0, 0, TimeSpan.Zero),
            LastModifiedBy = "test",
            LastModifiedAtUtc = new DateTimeOffset(2026, 4, 21, 10, 0, 0, TimeSpan.Zero)
        };

    private static (Guid First, Guid Second) Normalize(Guid userId1, Guid userId2) =>
        userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);

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
