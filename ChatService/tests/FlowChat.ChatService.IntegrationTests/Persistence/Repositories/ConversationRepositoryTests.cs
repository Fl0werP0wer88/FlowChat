using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.UserProfiles;
using FlowChat.ChatService.Persistence;
using FlowChat.Shared.Domain;
using FlowChat.ChatService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;
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
            "Latest");
        latestMessage.SetSequenceNumber(84);
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
    public async Task ConversationWriteRepository_GetByIdAsync_WhenConversationExists_ReturnsConversationWithParticipantsForAnyConversationType()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var createdByUserId = Guid.NewGuid();
        var groupMemberId = Guid.NewGuid();
        var duetPartnerId = Guid.NewGuid();
        var groupConversation = GroupConversation.Create(
            Id<Conversation>.New(),
            createdByUserId,
            [createdByUserId, groupMemberId],
            "Friends");
        var duetConversation = DuetConversation.Create(createdByUserId, duetPartnerId);
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
        groupResult!.Participants.Select(participant => participant.UserId.Value)
            .Should().BeEquivalentTo([createdByUserId, groupMemberId]);
        duetResult.Should().BeOfType<DuetConversation>();
        duetResult!.Participants.Select(participant => participant.UserId.Value)
            .Should().BeEquivalentTo([createdByUserId, duetPartnerId]);
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
    public async Task DuetConversationWriteRepository_GetByIdAsync_WhenDuetExists_ReturnsDuetWithParticipants()
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
        var repository = new DuetConversationWriteRepository(readContext);

        var result = await repository.GetByIdAsync(conversation.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(conversation.Id);
        result.Participants.Select(participant => participant.UserId.Value)
            .Should().BeEquivalentTo([requestingUserId, partnerUserId]);
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

    private static void MarkCreated(Conversation conversation)
    {
        conversation.SetCreated("integration-test");
        conversation.SetUpdated("integration-test");
    }

    private static void MarkCreated(ChatMessageAggregate message)
    {
        message.SetCreated("integration-test");
        message.SetUpdated("integration-test");
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
