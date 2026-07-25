using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Shared.Persistance;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.IntegrationTests.Persistence.Configuration;

public sealed class ReadSideV2ModelTests
{
    [Theory]
    [InlineData(typeof(ConversationReadEntityV2), "ConversationsV2")]
    [InlineData(typeof(ConversationMembershipReadEntityV2), "ConversationMembershipsV2")]
    [InlineData(typeof(ConversationParticipantReadEntityV2), "ConversationParticipantsV2")]
    [InlineData(typeof(DuetConversationReadEntityV2), "DuetConversationsV2")]
    [InlineData(typeof(ChatMessageReadEntityV2), "ChatMessagesV2")]
    public void Model_ReadEntityV2_MapsToExpectedV2View(Type entityType, string expectedView)
    {
        using var context = CreateDbContext();

        var modelEntity = context.Model.FindEntityType(entityType);

        modelEntity.Should().NotBeNull();
        modelEntity!.GetViewName().Should().Be(expectedView);
    }

    [Theory]
    [InlineData(typeof(ConversationReadEntityV2))]
    [InlineData(typeof(ConversationMembershipReadEntityV2))]
    [InlineData(typeof(ConversationParticipantReadEntityV2))]
    [InlineData(typeof(DuetConversationReadEntityV2))]
    [InlineData(typeof(ChatMessageReadEntityV2))]
    public void ReadEntityV2_UsesOnlyPersistenceReadTypes(Type entityType)
    {
        entityType.Should().BeDerivedFrom<ReadEntityBase>();
        entityType.GetProperties()
            .Select(property => property.PropertyType)
            .Should().OnlyContain(type => IsSimplePersistenceType(type));
    }

    private static bool IsSimplePersistenceType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive
               || underlyingType.IsEnum
               || underlyingType == typeof(Guid)
               || underlyingType == typeof(string)
               || underlyingType == typeof(DateTimeOffset)
               || underlyingType == typeof(decimal);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
