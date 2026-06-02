using System.Data.Common;
using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.ChatService.Persistence;

public sealed class AppDbContext : DbContext
{
    [ActivatorUtilitiesConstructor]
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbConnection connection)
        : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options)
    {
    }

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();
    public DbSet<UserProfileReadModelEntity> UserProfileProjections => Set<UserProfileReadModelEntity>();
    public DbSet<DuetConversationLookupEntity> DuetConversations => Set<DuetConversationLookupEntity>();
    public DbSet<ConversationReadEntity> ConversationReads => Set<ConversationReadEntity>();
    public DbSet<ParticipantUserReadEntity> ParticipantUserReads => Set<ParticipantUserReadEntity>();
    public DbSet<ChatMessageReadEntity> ChatMessageReads => Set<ChatMessageReadEntity>();
    public DbSet<DuetConversationReadEntity> DuetConversationReads => Set<DuetConversationReadEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
