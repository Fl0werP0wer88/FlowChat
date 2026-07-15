using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.RealtimeService.Persistence.Configuration.Entities;

public sealed class RealtimeGroupMembershipRevisionTrackerReadModelConfiguration
    : IEntityTypeConfiguration<RealtimeGroupMembershipRevisionTrackerReadModel>
{
    public void Configure(EntityTypeBuilder<RealtimeGroupMembershipRevisionTrackerReadModel> builder)
    {
        builder.ToTable("RealtimeGroupMembershipRevisionTrackerReadModels");

        builder.HasKey(x => x.ConversationId);

        builder.Property(x => x.Revision)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
