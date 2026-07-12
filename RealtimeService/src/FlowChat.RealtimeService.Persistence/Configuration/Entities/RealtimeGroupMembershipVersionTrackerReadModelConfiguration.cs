using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.RealtimeService.Persistence.Configuration.Entities;

public sealed class RealtimeGroupMembershipVersionTrackerReadModelConfiguration
    : IEntityTypeConfiguration<RealtimeGroupMembershipVersionTrackerReadModel>
{
    public void Configure(EntityTypeBuilder<RealtimeGroupMembershipVersionTrackerReadModel> builder)
    {
        builder.ToTable("RealtimeGroupMembershipVersionTrackerReadModels");

        builder.HasKey(x => x.ConversationId);

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
