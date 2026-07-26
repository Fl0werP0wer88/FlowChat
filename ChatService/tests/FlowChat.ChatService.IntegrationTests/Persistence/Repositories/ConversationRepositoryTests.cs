using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.ChatService.Persistence;
using FlowChat.Shared.Domain;
using FlowChat.ChatService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ChatMessageAggregateV2 = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessageV2;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ConversationRepositoryTests
{
    [Fact]
    public async Task GroupConversationReadRepository_GetByParticipantUserIdAsync_ReturnsSequenceNumbersForRequestedParticipant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var creatorUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var requestedUserId = Guid.NewGuid();
        var matchingConversation = ConversationV2.CreateGroup(
            Id<ConversationV2>.New(),
            Id<UserProfile>.FromGuid(creatorUserId),
            [Id<UserProfile>.FromGuid(memberUserId), Id<UserProfile>.FromGuid(requestedUserId)],
            "Friends");
        var participants = new[]
        {
            CreateGroupParticipant(matchingConversation.Id, creatorUserId),
            CreateGroupParticipant(matchingConversation.Id, memberUserId),
            CreateGroupParticipant(matchingConversation.Id, requestedUserId, lastReadSequence: 42)
        };
        var latestMessage = ChatMessageAggregateV2.Create(
            Id<ChatMessageAggregateV2>.New(),
            matchingConversation.Id,
            Id<UserProfile>.FromGuid(creatorUserId),
            "Latest",
            sequenceNum: 84);
        MarkCreated(latestMessage);

        MarkCreated(matchingConversation);
        foreach (var participant in participants)
        {
            MarkCreated(participant);
        }

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.ConversationsV2.Add(matchingConversation);
            seedContext.ConversationParticipantsV2.AddRange(participants);
            seedContext.ChatMessagesV2.Add(latestMessage);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new GroupConversationReadRepository(readContext);

        var result = await repository.GetByParticipantUserIdAsync(requestedUserId, CancellationToken.None);

        var summary = result.Should().ContainSingle().Subject;
        summary.ConversationId.Should().Be(matchingConversation.Id.Value);
        summary.Name.Should().Be("Friends");
        summary.ParticipantCount.Should().Be(3);
        summary.LastReadMsgSeqNum.Should().Be(42);
        summary.CurrentMsgSeqNum.Should().Be(84);
    }

    [Fact]
    public async Task GroupConversationReadRepository_GetByParticipantUserIdAsync_WhenConversationHasNoMessages_ReturnsZeroCurrentSequenceNumber()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = ConversationV2.CreateGroup(
            Id<ConversationV2>.New(),
            Id<UserProfile>.FromGuid(requestingUserId),
            [Id<UserProfile>.FromGuid(partnerUserId)],
            "Empty conversation");
        var participants = new[]
        {
            CreateGroupParticipant(conversation.Id, requestingUserId),
            CreateGroupParticipant(conversation.Id, partnerUserId)
        };
        MarkCreated(conversation);
        foreach (var participant in participants)
        {
            MarkCreated(participant);
        }

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.ConversationsV2.Add(conversation);
            seedContext.ConversationParticipantsV2.AddRange(participants);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var result = await new GroupConversationReadRepository(readContext)
            .GetByParticipantUserIdAsync(requestingUserId, CancellationToken.None);

        result.Should().ContainSingle().Which.CurrentMsgSeqNum.Should().Be(0);
    }

    [Fact]
    public async Task ConversationParticipantReadRepository_GetParticipantUserIdsAsync_ReturnsParticipantsForAnyConversationType()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var requestingUserId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversation = ConversationV2.CreateDuet(
            Id<UserProfile>.FromGuid(requestingUserId),
            Id<UserProfile>.FromGuid(partnerUserId));
        var participants = new[]
        {
            CreateDuetParticipant(conversation.Id, requestingUserId, partnerUserId),
            CreateDuetParticipant(conversation.Id, partnerUserId, requestingUserId)
        };
        MarkCreated(conversation);
        foreach (var participant in participants)
        {
            MarkCreated(participant);
        }

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.ConversationsV2.Add(conversation);
            seedContext.ConversationParticipantsV2.AddRange(participants);
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

    private static ConversationParticipant CreateGroupParticipant(
        Id<ConversationV2> conversationId,
        Guid userId,
        long lastReadSequence = 0) =>
        ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            ConversationType.Group,
            Id<UserProfile>.FromGuid(userId),
            duetPartnerUserId: null,
            lastReadMessageSequenceNum: lastReadSequence);

    private static ConversationParticipant CreateDuetParticipant(
        Id<ConversationV2> conversationId,
        Guid userId,
        Guid partnerUserId) =>
        ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            conversationId,
            ConversationType.Duet,
            Id<UserProfile>.FromGuid(userId),
            Id<UserProfile>.FromGuid(partnerUserId));

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
}
