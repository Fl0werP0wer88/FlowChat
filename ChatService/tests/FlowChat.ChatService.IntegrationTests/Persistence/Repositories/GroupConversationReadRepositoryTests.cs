using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class GroupConversationReadRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ActiveGroup_ReturnsActiveParticipantsWithProfileFallback()
    {
        var conversationId = Guid.NewGuid();
        var namedUserId = Guid.NewGuid();
        var profileUserId = Guid.NewGuid();
        var deletedParticipant = CreateParticipant(conversationId, Guid.NewGuid(), "Deleted");
        deletedParticipant.DeletedAt = DateTimeOffset.UtcNow;
        await using var context = CreateDbContext();
        context.ConversationReadsV2.Add(CreateConversation(conversationId, "Project"));
        context.ConversationParticipantReadsV2.AddRange(
            CreateParticipant(conversationId, namedUserId, "Custom name"),
            CreateParticipant(conversationId, profileUserId, null),
            deletedParticipant);
        context.UserProfileProjections.Add(CreateProfile(profileUserId, "Ada", "Lovelace"));
        await context.SaveChangesAsync();

        var result = await new GroupConversationReadRepository(context)
            .GetByIdAsync(conversationId);

        result.Should().NotBeNull();
        result!.ConversationId.Should().Be(conversationId);
        result.Name.Should().Be("Project");
        result.Participants.Should().HaveCount(2);
        result.Participants.Should().ContainSingle(x =>
            x.UserId == namedUserId && x.DisplayName == "Custom name");
        result.Participants.Should().ContainSingle(x =>
            x.UserId == profileUserId && x.DisplayName == "Ada Lovelace");
    }

    [Fact]
    public async Task GetByParticipantUserIdAsync_ActiveGroup_ReturnsV2ReadCursorCountAndSequence()
    {
        var conversationId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var deletedMessage = CreateMessage(conversationId, 99);
        deletedMessage.DeletedAt = DateTimeOffset.UtcNow;
        await using var context = CreateDbContext();
        context.ConversationReadsV2.Add(CreateConversation(conversationId, "Project"));
        context.ConversationParticipantReadsV2.AddRange(
            CreateParticipant(conversationId, requestingUserId, "Me", lastReadSequence: 4),
            CreateParticipant(conversationId, Guid.NewGuid(), "Other"),
            CreateParticipant(conversationId, Guid.NewGuid(), "Third"));
        context.ChatMessageReadsV2.AddRange(
            CreateMessage(conversationId, 5),
            CreateMessage(conversationId, 8),
            deletedMessage);
        await context.SaveChangesAsync();

        var result = await new GroupConversationReadRepository(context)
            .GetByParticipantUserIdAsync(requestingUserId);

        var summary = result.Should().ContainSingle().Subject;
        summary.ConversationId.Should().Be(conversationId);
        summary.ParticipantCount.Should().Be(3);
        summary.LastReadMsgSeqNum.Should().Be(4);
        summary.CurrentMsgSeqNum.Should().Be(8);
    }

    [Fact]
    public async Task GetByIdAsync_DeletedConversation_ReturnsNull()
    {
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(conversationId, "Deleted");
        conversation.DeletedAt = DateTimeOffset.UtcNow;
        await using var context = CreateDbContext();
        context.ConversationReadsV2.Add(conversation);
        context.ConversationParticipantReadsV2.Add(
            CreateParticipant(conversationId, Guid.NewGuid(), "Participant"));
        await context.SaveChangesAsync();

        var result = await new GroupConversationReadRepository(context)
            .GetByIdAsync(conversationId);

        result.Should().BeNull();
    }

    private static ConversationReadEntityV2 CreateConversation(Guid conversationId, string name) =>
        new()
        {
            Id = conversationId,
            ConversationType = 2,
            Name = name,
            Version = 2
        };

    private static ConversationParticipantReadEntityV2 CreateParticipant(
        Guid conversationId,
        Guid userId,
        string? displayName,
        long lastReadSequence = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            ConversationType = 2,
            UserId = userId,
            DisplayName = displayName,
            LastReadMessageSequenceNum = lastReadSequence
        };

    private static ChatMessageReadEntityV2 CreateMessage(Guid conversationId, long sequenceNumber) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = Guid.NewGuid(),
            Text = $"Message {sequenceNumber}",
            SentAtUtc = DateTimeOffset.UtcNow.AddMinutes(sequenceNumber),
            SequenceNum = sequenceNumber
        };

    private static UserProfileReadModelEntity CreateProfile(
        Guid userId,
        string firstName,
        string lastName) =>
        new()
        {
            UserId = userId,
            FriendlyUserId = userId.ToString("N"),
            FirstName = firstName,
            LastName = lastName,
            SourceVersion = 1,
            SourceCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceLastModifiedAtUtc = DateTimeOffset.UtcNow
        };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
