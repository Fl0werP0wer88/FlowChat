using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.ValueObjects;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlowChat.ChatService.IntegrationTests.Persistence;

public sealed class V2ModelConfigurationTests
{
    [Fact]
    public void Model_V2Entities_UsesSeparateTablesAndAggregateConcurrencyTokens()
    {
        using var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=localhost;Database=model_only;Username=test;Password=test")
                .Options);

        var model = context.Model;
        model.FindEntityType(typeof(ConversationV2))!.GetTableName().Should().Be("ConversationsV2");
        model.FindEntityType(typeof(ConversationMembership))!.GetTableName().Should().Be("ConversationMembershipsV2");
        model.FindEntityType(typeof(ConversationParticipant))!.GetTableName().Should().Be("ConversationParticipantsV2");
        model.FindEntityType(typeof(ChatMessageV2))!.GetTableName().Should().Be("ChatMessagesV2");
        model.FindEntityType(typeof(ConversationMessageSequenceEntityV2))!.GetTableName()
            .Should().Be("ConversationMessageSequencesV2");

        foreach (var aggregateType in new[]
                 {
                     typeof(ConversationV2),
                     typeof(ConversationMembership),
                     typeof(ConversationParticipant),
                     typeof(ChatMessageV2)
                 })
        {
            model.FindEntityType(aggregateType)!
                .FindProperty("Version")!
                .IsConcurrencyToken.Should().BeTrue();
        }

        model.FindEntityType(typeof(ConversationMessageSequenceEntityV2))!
            .FindProperty("Version")
            .Should().BeNull();
        model.FindEntityType(typeof(ConversationParticipant))!
            .FindProperty(nameof(ConversationParticipant.ConversationType))!
            .IsNullable.Should().BeFalse();
        model.FindEntityType(typeof(ConversationParticipant))!
            .FindProperty(nameof(ConversationParticipant.DuetPartnerUserId))!
            .IsNullable.Should().BeTrue();
        model.FindEntityType(typeof(ChatMessageV2))!
            .FindProperty("RecipientUserIds")
            .Should().BeNull();

        var conversationEntity = model.FindEntityType(typeof(ConversationV2))!;
        conversationEntity.FindProperty("CreatedByUserId").Should().BeNull();

        var duetPairEntity = model.FindEntityType(typeof(DuetParticipantPair))!;
        duetPairEntity.GetIndexes().Should().ContainSingle(index =>
            index.GetDatabaseName() == "UX_ConversationsV2_DuetParticipantPair" &&
            index.IsUnique &&
            index.GetFilter() == "\"DeletedAt\" IS NULL AND \"ConversationType\" = 1");

        var conversationsTable = StoreObjectIdentifier.Table("ConversationsV2", null);
        duetPairEntity.FindProperty(nameof(DuetParticipantPair.FirstUserId))!
            .GetColumnName(conversationsTable).Should().Be("DuetFirstUserId");
        duetPairEntity.FindProperty(nameof(DuetParticipantPair.SecondUserId))!
            .GetColumnName(conversationsTable).Should().Be("DuetSecondUserId");
    }
}
