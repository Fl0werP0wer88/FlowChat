using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ChatMessageAggregateV2 = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessageV2;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class DuetConversationReadRepositoryTests
{
    [Fact]
    public async Task GetByUserIdsAsync_WhenDuetConversationExists_ReturnsParticipantDetails()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var duet = CreateDuet(requestingUserId, partnerUserId);
        duet.Participants[0].ChangeDisplayName("Requester override");

        await using (var seedContext = CreateDbContext(connection))
        {
            AddDuet(seedContext, duet);
            seedContext.UserProfileProjections.AddRange(
                CreateProfile(requestingUserId, "Requester", avatarUrl: "requester.png"),
                CreateProfile(partnerUserId, "Partner", avatarUrl: "partner.png"));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var result = await new DuetConversationReadRepository(readContext)
            .GetByUserIdsAsync(requestingUserId, partnerUserId);

        result.Should().NotBeNull();
        result!.ConversationId.Should().Be(duet.Conversation.Id.Value);
        result.Participants.Should().HaveCount(2);
        result.Participants.Should().ContainSingle(x =>
            x.UserId == requestingUserId
            && x.DisplayName == "Requester override"
            && x.AvatarUrl == "requester.png");
        result.Participants.Should().ContainSingle(x =>
            x.UserId == partnerUserId
            && x.DisplayName == "Partner"
            && x.AvatarUrl == "partner.png");
    }

    [Fact]
    public async Task GetByUserIdsAsync_WhenProfileIsDeleted_ReturnsFallbackValues()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var duet = CreateDuet(requestingUserId, partnerUserId);

        await using (var seedContext = CreateDbContext(connection))
        {
            AddDuet(seedContext, duet);
            seedContext.UserProfileProjections.AddRange(
                CreateProfile(requestingUserId, "Requester", avatarUrl: "requester.png"),
                CreateProfile(
                    partnerUserId,
                    "Deleted",
                    avatarUrl: "deleted.png",
                    deletedAt: DateTimeOffset.UtcNow));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var result = await new DuetConversationReadRepository(readContext)
            .GetByUserIdsAsync(requestingUserId, partnerUserId);

        var partner = result!.Participants.Single(x => x.UserId == partnerUserId);
        partner.DisplayName.Should().BeNull();
        partner.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdsAsync_WhenDuetLookupPointsToMissingConversation_ReturnsNull()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var (first, second) = Normalize(requestingUserId, partnerUserId);
        var missingConversationId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(connection))
        {
            await seedContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
            await seedContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "DuetConversationsV2" ("FirstUserId", "SecondUserId", "ConversationId")
                VALUES ({first}, {second}, {missingConversationId})
                """);
            await seedContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        await using var readContext = CreateDbContext(connection);
        var result = await new DuetConversationReadRepository(readContext)
            .GetByUserIdsAsync(requestingUserId, partnerUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContactsForUserAsync_WhenPartnerBlockedRequester_ReturnsParticipantState()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var duet = CreateDuet(requestingUserId, partnerUserId);
        duet.Participants[0].AdvanceReadCursor(3);
        duet.Participants[0].Mute();
        duet.Participants[1].Block();
        var message = ChatMessageAggregateV2.Create(
            Id<ChatMessageAggregateV2>.New(),
            duet.Conversation.Id,
            Id<UserProfile>.FromGuid(partnerUserId),
            "Latest",
            sequenceNum: 12);
        MarkCreated(message);

        await using (var seedContext = CreateDbContext(connection))
        {
            AddDuet(seedContext, duet);
            seedContext.ChatMessagesV2.Add(message);
            seedContext.UserProfileProjections.Add(
                CreateProfile(
                    partnerUserId,
                    "Partner",
                    avatarUrl: "partner.png",
                    email: "partner@example.com"));
            await seedContext.SaveChangesAsync();
            await seedContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "ConversationMessageSequencesV2" ("ConversationId", "LastAssignedSequenceNum")
                VALUES ({duet.Conversation.Id.Value}, {12L})
                """);
        }

        await using var readContext = CreateDbContext(connection);
        var result = await new DuetConversationReadRepository(readContext)
            .GetContactsForUserAsync(requestingUserId);

        var contact = result.Should().ContainSingle().Subject;
        contact.PartnerUserId.Should().Be(partnerUserId);
        contact.DisplayName.Should().Be("Partner");
        contact.AvatarUrl.Should().Be("partner.png");
        contact.Email.Should().Be("partner@example.com");
        contact.ConversationId.Should().Be(duet.Conversation.Id.Value);
        contact.IsBlocked.Should().BeFalse();
        contact.IsBlockedByPartner.Should().BeTrue();
        contact.IsMuted.Should().BeTrue();
        contact.IsHidden.Should().BeFalse();
        contact.LastReadMsgSeqNum.Should().Be(3);
        contact.CurrentMsgSeqNum.Should().Be(12);
    }

    [Fact]
    public async Task GetContactsForUserAsync_WhenRequesterHiddenContact_ReturnsNoContact()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var duet = CreateDuet(requestingUserId, partnerUserId);
        duet.Participants[0].Hide();

        await using (var seedContext = CreateDbContext(connection))
        {
            AddDuet(seedContext, duet);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var result = await new DuetConversationReadRepository(readContext)
            .GetContactsForUserAsync(requestingUserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdsAsync_WhenPartnerParticipantDeleted_ReturnsNull()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var duet = CreateDuet(requestingUserId, partnerUserId);
        duet.Participants[1].Delete(UtcDateTimeOffset.UtcNow);

        await using (var seedContext = CreateDbContext(connection))
        {
            AddDuet(seedContext, duet);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var result = await new DuetConversationReadRepository(readContext)
            .GetByUserIdsAsync(requestingUserId, partnerUserId);

        result.Should().BeNull();
    }

    private static DuetData CreateDuet(Guid requestingUserId, Guid partnerUserId)
    {
        var conversation = ConversationV2.CreateDuet(
            Id<UserProfile>.FromGuid(requestingUserId),
            Id<UserProfile>.FromGuid(partnerUserId));
        var participants = new[]
        {
            ConversationParticipant.Create(
                Id<ConversationParticipant>.New(),
                conversation.Id,
                ConversationType.Duet,
                Id<UserProfile>.FromGuid(requestingUserId),
                Id<UserProfile>.FromGuid(partnerUserId)),
            ConversationParticipant.Create(
                Id<ConversationParticipant>.New(),
                conversation.Id,
                ConversationType.Duet,
                Id<UserProfile>.FromGuid(partnerUserId),
                Id<UserProfile>.FromGuid(requestingUserId))
        };
        var (first, second) = Normalize(requestingUserId, partnerUserId);
        var lookup = new DuetConversationLookupEntityV2
        {
            ConversationId = conversation.Id,
            FirstUserId = first,
            SecondUserId = second
        };

        MarkCreated(conversation);
        foreach (var participant in participants)
        {
            MarkCreated(participant);
        }

        return new DuetData(conversation, participants, lookup);
    }

    private static void AddDuet(AppDbContext context, DuetData duet)
    {
        context.ConversationsV2.Add(duet.Conversation);
        context.ConversationParticipantsV2.AddRange(duet.Participants);
        context.DuetConversationsV2.Add(duet.Lookup);
    }

    private static UserProfileReadModelEntity CreateProfile(
        Guid userId,
        string firstName,
        string? avatarUrl = null,
        string? email = null,
        DateTimeOffset? deletedAt = null) =>
        new()
        {
            UserId = userId,
            FriendlyUserId = userId.ToString("N"),
            FirstName = firstName,
            AvatarUrl = avatarUrl,
            Email = email,
            SourceVersion = 1,
            SourceCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceLastModifiedAtUtc = DateTimeOffset.UtcNow,
            SourceDeletedAtUtc = deletedAt
        };

    private static (Guid First, Guid Second) Normalize(Guid firstUserId, Guid secondUserId) =>
        firstUserId.CompareTo(secondUserId) < 0
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);

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

    private static void MarkCreated(ConversationV2 conversation)
    {
        conversation.SetCreated("integration-test");
        conversation.SetUpdated("integration-test");
    }

    private static void MarkCreated(ConversationParticipant participant)
    {
        participant.SetCreated("integration-test");
        participant.SetUpdated("integration-test");
    }

    private static void MarkCreated(ChatMessageAggregateV2 message)
    {
        message.SetCreated("integration-test");
        message.SetUpdated("integration-test");
    }

    private sealed record DuetData(
        ConversationV2 Conversation,
        IReadOnlyList<ConversationParticipant> Participants,
        DuetConversationLookupEntityV2 Lookup);
}
