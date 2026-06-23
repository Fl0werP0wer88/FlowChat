using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<Conversation>.FromGuid(x));

        builder.Property(x => x.Type)
            .IsRequired();

        builder.HasDiscriminator(x => x.Type)
            .HasValue<DuetConversation>(ConversationType.Duet)
            .HasValue<GroupConversation>(ConversationType.Group);

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasUtcDateTimeOffsetConversion();

        builder.Ignore(x => x.DomainEvents);

        builder.HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
