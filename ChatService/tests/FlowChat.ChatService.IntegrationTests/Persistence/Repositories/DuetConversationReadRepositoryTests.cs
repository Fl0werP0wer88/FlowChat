using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class DuetConversationReadRepositoryTests
{
    [Fact]
    public async Task GetConversationsForContactsAsync_WhenDuetConversationExists_ReturnsConversationSequenceFields()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var conversationId = Id<Conversation>.New();
        var conversation = DuetConversation.Restore(
            conversationId,
            Id<FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile>.FromGuid(requestingUserId),
            lastMsgSequenceNum: 84,
            [
                ParticipantUser.Restore(
                    Id<ParticipantUser>.New(),
                    conversationId,
                    Id<FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile>.FromGuid(requestingUserId),
                    displayName: null,
                    avatarUrl: null,
                    isBlocked: false,
                    isMuted: false,
                    isHidden: false,
                    UtcDateTimeOffset.UtcNow,
                    lastReadMessageSequenceNum: 42),
                ParticipantUser.Restore(
                    Id<ParticipantUser>.New(),
                    conversationId,
                    Id<FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile>.FromGuid(partnerUserId),
                    displayName: null,
                    avatarUrl: null,
                    isBlocked: false,
                    isMuted: false,
                    isHidden: false,
                    UtcDateTimeOffset.UtcNow,
                    lastReadMessageSequenceNum: 80)
            ]);
        MarkCreated(conversation);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.DuetConversations.Add(CreateDuetConversation(requestingUserId, partnerUserId, conversation.Id.Value));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new DuetConversationReadRepository(readContext);

        var result = await repository.GetConversationsForContactsAsync(
            requestingUserId,
            [partnerUserId, otherUserId],
            CancellationToken.None);

        var conversationForContact = result.Should().ContainSingle().Subject;
        conversationForContact.PartnerUserId.Should().Be(partnerUserId);
        conversationForContact.ConversationId.Should().Be(conversation.Id.Value);
        conversationForContact.LastReadMsgSeqNum.Should().Be(42);
        conversationForContact.CurrentMsgSeqNum.Should().Be(84);
    }

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
        MarkCreated(conversation);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.DuetConversations.Add(CreateDuetConversation(requestingUserId, partnerUserId, conversation.Id.Value));
            seedContext.UserProfileProjections.AddRange(
                CreateProfile(requestingUserId, "requester", firstName: "Requester", avatarUrl: "requester.png"),
                CreateProfile(partnerUserId, "partner", firstName: "Partner", avatarUrl: "partner.png"));

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
        MarkCreated(conversation);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.DuetConversations.Add(CreateDuetConversation(requestingUserId, partnerUserId, conversation.Id.Value));
            seedContext.UserProfileProjections.AddRange(
                CreateProfile(requestingUserId, "requester", firstName: "Requester", avatarUrl: "requester.png"),
                CreateProfile(
                    partnerUserId,
                    "deleted-partner",
                    firstName: "Deleted",
                    avatarUrl: "deleted.png",
                    deletedAt: new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero)));

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

    [Fact]
    public async Task GetContactsForUserAsync_WhenPartnerHasBlockedRequester_ReturnsIsBlockedByPartnerTrue()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation.Create(
            createdByUserId: requestingUserId,
            partnerUserId: partnerUserId);
        conversation.MarkParticipantAsRead(requestingUserId);
        conversation.BlockParticipant(partnerUserId);
        MarkCreated(conversation);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.DuetConversations.Add(CreateDuetConversation(requestingUserId, partnerUserId, conversation.Id.Value));
            seedContext.UserProfileProjections.Add(
                CreateProfile(partnerUserId, "partner", firstName: "Partner", avatarUrl: "partner.png", email: "partner@example.com"));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new DuetConversationReadRepository(readContext);

        var result = await repository.GetContactsForUserAsync(requestingUserId, CancellationToken.None);

        var contact = result.Should().ContainSingle().Subject;
        contact.PartnerUserId.Should().Be(partnerUserId);
        contact.DisplayName.Should().Be("Partner");
        contact.AvatarUrl.Should().Be("partner.png");
        contact.Email.Should().Be("partner@example.com");
        contact.ConversationId.Should().Be(conversation.Id.Value);
        contact.IsBlocked.Should().BeFalse();
        contact.IsBlockedByPartner.Should().BeTrue();
        contact.IsMuted.Should().BeFalse();
        contact.IsHidden.Should().BeFalse();
    }

    [Fact]
    public async Task GetContactsForUserAsync_WhenRequesterHasHiddenContact_ExcludesItFromResults()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = FlowChat.ChatService.Domain.Entities.Conversation.DuetConversation.Create(
            createdByUserId: requestingUserId,
            partnerUserId: partnerUserId);
        conversation.HideParticipant(requestingUserId);
        MarkCreated(conversation);

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Conversations.Add(conversation);
            seedContext.DuetConversations.Add(CreateDuetConversation(requestingUserId, partnerUserId, conversation.Id.Value));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new DuetConversationReadRepository(readContext);

        var result = await repository.GetContactsForUserAsync(requestingUserId, CancellationToken.None);

        result.Should().BeEmpty();
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

    private static UserProfileReadModelEntity CreateProfile(
        Guid userId,
        string friendlyUserId,
        string? firstName = null,
        string? lastName = null,
        string? avatarUrl = null,
        string? email = null,
        DateTimeOffset? deletedAt = null) =>
        new()
        {
            UserId = userId,
            FriendlyUserId = friendlyUserId,
            FirstName = firstName,
            LastName = lastName,
            AvatarUrl = avatarUrl,
            Email = email,
            SourceDeletedAtUtc = deletedAt
        };

    private static (Guid First, Guid Second) Normalize(Guid userId1, Guid userId2) =>
        userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);

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
