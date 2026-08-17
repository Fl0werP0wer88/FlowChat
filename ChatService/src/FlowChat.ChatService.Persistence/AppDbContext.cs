using System.Data.Common;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.ChatService.Persistence;

public sealed class AppDbContext : DbContext
{
    [ActivatorUtilitiesConstructor]
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Silverback uses the active connection so offset and outbox writes share the business transaction
    public AppDbContext(DbConnection connection)
        : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options)
    {
    }

    public DbSet<ChatMessageV2> ChatMessagesV2 => Set<ChatMessageV2>();
    public DbSet<ConversationV2> ConversationsV2 => Set<ConversationV2>();
    public DbSet<ConversationMembership> ConversationMembershipsV2 => Set<ConversationMembership>();
    public DbSet<ConversationParticipant> ConversationParticipantsV2 => Set<ConversationParticipant>();
    public DbSet<ConversationMessageSequenceEntityV2> ConversationMessageSequencesV2 => Set<ConversationMessageSequenceEntityV2>();
    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();
    public DbSet<UserProfileReadModelEntity> UserProfileProjections => Set<UserProfileReadModelEntity>();
    public DbSet<ConversationReadEntityV2> ConversationReadsV2 => Set<ConversationReadEntityV2>();
    public DbSet<ConversationMembershipReadEntityV2> ConversationMembershipReadsV2 => Set<ConversationMembershipReadEntityV2>();
    public DbSet<ConversationParticipantReadEntityV2> ConversationParticipantReadsV2 => Set<ConversationParticipantReadEntityV2>();
    public DbSet<ChatMessageReadEntityV2> ChatMessageReadsV2 => Set<ChatMessageReadEntityV2>();
    public DbSet<ConversationMessageSequenceReadEntityV2> ConversationMessageSequenceReadsV2 => Set<ConversationMessageSequenceReadEntityV2>();
    public DbSet<SilverbackStoredOffset> SilverbackStoredOffsets => Set<SilverbackStoredOffset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
