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

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatMessageV2> ChatMessagesV2 => Set<ChatMessageV2>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationV2> ConversationsV2 => Set<ConversationV2>();
    public DbSet<ConversationMembership> ConversationMembershipsV2 => Set<ConversationMembership>();
    public DbSet<ConversationParticipant> ConversationParticipantsV2 => Set<ConversationParticipant>();
    public DbSet<ConversationMessageSequenceEntity> ConversationMessageSequences => Set<ConversationMessageSequenceEntity>();
    public DbSet<ConversationMessageSequenceEntityV2> ConversationMessageSequencesV2 => Set<ConversationMessageSequenceEntityV2>();
    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();
    public DbSet<UserProfileReadModelEntity> UserProfileProjections => Set<UserProfileReadModelEntity>();
    public DbSet<DuetConversationLookupEntity> DuetConversations => Set<DuetConversationLookupEntity>();
    public DbSet<DuetConversationLookupEntityV2> DuetConversationsV2 => Set<DuetConversationLookupEntityV2>();
    public DbSet<ConversationReadEntity> ConversationReads => Set<ConversationReadEntity>();
    public DbSet<ParticipantUserReadEntity> ParticipantUserReads => Set<ParticipantUserReadEntity>();
    public DbSet<ChatMessageReadEntity> ChatMessageReads => Set<ChatMessageReadEntity>();
    public DbSet<DuetConversationReadEntity> DuetConversationReads => Set<DuetConversationReadEntity>();
    public DbSet<SilverbackStoredOffset> SilverbackStoredOffsets => Set<SilverbackStoredOffset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
