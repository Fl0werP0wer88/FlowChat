using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationV2Configuration : IEntityTypeConfiguration<ConversationV2>
{
    public void Configure(EntityTypeBuilder<ConversationV2> builder)
    {
        builder.ToTable("ConversationsV2");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ConversationV2>.FromGuid(x));

        builder.Property(x => x.ConversationType)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedByUserId)
            .HasConversion(x => x.Value, x => Id<UserProfileMarker>.FromGuid(x))
            .IsRequired();

        ConfigureAggregate(builder);

        builder.HasIndex(x => x.ConversationType)
            .HasFilter("\"DeletedAt\" IS NULL");
    }

    private static void ConfigureAggregate(EntityTypeBuilder<ConversationV2> builder)
    {
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.LastModifiedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.LastModifiedAtUtc).HasUtcDateTimeOffsetConversion();
        builder.Property(x => x.DeletedAt).HasNullableUtcDateTimeOffsetConversion();
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DomainEvents);
    }
}
