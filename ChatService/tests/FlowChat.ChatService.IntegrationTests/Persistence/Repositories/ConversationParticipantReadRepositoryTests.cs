using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.ChatService.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Repositories;

public sealed class ConversationParticipantReadRepositoryTests
{
    [Fact]
    public async Task GetParticipantUserIdsAsync_ActiveConversation_ReturnsOnlyActiveV2Participants()
    {
        var conversationId = Guid.NewGuid();
        var activeUserId = Guid.NewGuid();
        var deletedParticipant = CreateParticipant(conversationId, Guid.NewGuid());
        deletedParticipant.DeletedAt = DateTimeOffset.UtcNow;
        await using var context = CreateDbContext();
        context.ConversationReadsV2.Add(CreateConversation(conversationId));
        context.ConversationParticipantReadsV2.AddRange(
            CreateParticipant(conversationId, activeUserId),
            deletedParticipant);
        await context.SaveChangesAsync();

        var result = await new ConversationParticipantReadRepository(context)
            .GetParticipantUserIdsAsync(conversationId);

        result.Should().Equal(activeUserId);
    }

    [Fact]
    public async Task GetParticipantStatesAsync_DeletedConversation_ReturnsNull()
    {
        var conversationId = Guid.NewGuid();
        var conversation = CreateConversation(conversationId);
        conversation.DeletedAt = DateTimeOffset.UtcNow;
        await using var context = CreateDbContext();
        context.ConversationReadsV2.Add(conversation);
        context.ConversationParticipantReadsV2.Add(CreateParticipant(conversationId, Guid.NewGuid()));
        await context.SaveChangesAsync();

        var result = await new ConversationParticipantReadRepository(context)
            .GetParticipantStatesAsync(conversationId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMembershipRevisionAsync_ActiveMembership_ReturnsMembershipAggregateVersion()
    {
        var conversationId = Guid.NewGuid();
        await using var context = CreateDbContext();
        context.ConversationReadsV2.Add(CreateConversation(conversationId));
        context.ConversationMembershipReadsV2.Add(new ConversationMembershipReadEntityV2
        {
            Id = conversationId,
            ConversationId = conversationId,
            ConversationType = 2,
            ParticipantCount = 3,
            Version = 7
        });
        await context.SaveChangesAsync();

        var result = await new ConversationParticipantReadRepository(context)
            .GetMembershipRevisionAsync(conversationId);

        result.Should().Be(7);
    }

    [Fact]
    public async Task GetMembershipRevisionAsync_DeletedMembership_ReturnsNull()
    {
        var conversationId = Guid.NewGuid();
        await using var context = CreateDbContext();
        context.ConversationReadsV2.Add(CreateConversation(conversationId));
        context.ConversationMembershipReadsV2.Add(new ConversationMembershipReadEntityV2
        {
            Id = conversationId,
            ConversationId = conversationId,
            ConversationType = 2,
            ParticipantCount = 2,
            Version = 4,
            DeletedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await new ConversationParticipantReadRepository(context)
            .GetMembershipRevisionAsync(conversationId);

        result.Should().BeNull();
    }

    private static ConversationReadEntityV2 CreateConversation(Guid conversationId) =>
        new()
        {
            Id = conversationId,
            ConversationType = 2,
            Name = "Group",
            Version = 2
        };

    private static ConversationParticipantReadEntityV2 CreateParticipant(
        Guid conversationId,
        Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            ConversationType = 2,
            UserId = userId,
            DisplayName = "Participant",
            LastReadMessageSequenceNum = 0
        };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
