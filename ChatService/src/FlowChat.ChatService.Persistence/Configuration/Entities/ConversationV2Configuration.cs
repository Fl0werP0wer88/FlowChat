using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.ValueObjects;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Persistence.Configuration.Entities;

public sealed class ConversationV2Configuration : IEntityTypeConfiguration<ConversationV2>
{
    internal const string DuetParticipantPairUniqueIndexName =
        "UX_ConversationsV2_DuetParticipantPair";

    public void Configure(EntityTypeBuilder<ConversationV2> builder)
    {
        builder.ToTable("ConversationsV2", table =>
        {
            table.HasCheckConstraint(
                "CK_ConversationsV2_DuetParticipantShape",
                "(\"ConversationType\" = 1 AND \"DuetFirstUserId\" IS NOT NULL AND \"DuetSecondUserId\" IS NOT NULL) OR " +
                "(\"ConversationType\" = 2 AND \"DuetFirstUserId\" IS NULL AND \"DuetSecondUserId\" IS NULL)");
            table.HasCheckConstraint(
                "CK_ConversationsV2_NormalizedDuetParticipants",
                "\"DuetFirstUserId\" IS NULL OR \"DuetFirstUserId\" < \"DuetSecondUserId\"");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ConversationV2>.FromGuid(x));

        builder.Property(x => x.ConversationType)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        ConfigureDuetParticipants(builder);

        ConfigureAggregate(builder);

        builder.HasIndex(x => x.ConversationType)
            .HasFilter("\"DeletedAt\" IS NULL");
    }

    private static void ConfigureDuetParticipants(EntityTypeBuilder<ConversationV2> builder)
    {
        builder.OwnsOne(x => x.DuetParticipants, duetParticipants =>
        {
            duetParticipants.Property(x => x.FirstUserId)
                .HasColumnName("DuetFirstUserId")
                .HasConversion(x => x.Value, x => Id<UserProfileMarker>.FromGuid(x));
            duetParticipants.Property(x => x.SecondUserId)
                .HasColumnName("DuetSecondUserId")
                .HasConversion(x => x.Value, x => Id<UserProfileMarker>.FromGuid(x));

            duetParticipants.HasIndex(x => new { x.FirstUserId, x.SecondUserId })
                .IsUnique()
                .HasDatabaseName(DuetParticipantPairUniqueIndexName)
                .HasFilter("\"DeletedAt\" IS NULL AND \"ConversationType\" = 1");
        });

        builder.Navigation(x => x.DuetParticipants).IsRequired(false);
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
