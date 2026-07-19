using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationMembershipConfiguration
    : IEntityTypeConfiguration<ConversationMembership>
{
    public void Configure(EntityTypeBuilder<ConversationMembership> builder)
    {
        builder.ToTable("ConversationMembershipsV2");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ConversationMembership>.FromGuid(x));

        builder.Property(x => x.ConversationId)
            .HasConversion(x => x.Value, x => Id<ConversationV2>.FromGuid(x))
            .IsRequired();

        builder.Property(x => x.ConversationType).IsRequired();
        builder.Property(x => x.ParticipantCount).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.LastModifiedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.LastModifiedAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.DeletedAt).HasNullableUtcDateTimeOffsetConversion();
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => x.ConversationId).IsUnique();

        builder.HasOne<ConversationV2>()
            .WithOne()
            .HasForeignKey<ConversationMembership>(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
