using FlowChat.RealtimeService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.RealtimeService.Persistence.Configuration.Entities;

public sealed class RealtimeGroupMembershipReadModelConfiguration : IEntityTypeConfiguration<RealtimeGroupMembershipReadModel>
{
    public void Configure(EntityTypeBuilder<RealtimeGroupMembershipReadModel> builder)
    {
        builder.ToTable("RealtimeGroupMembershipReadModels");

        // UserId leads the composite key so lookups by UserId alone (fired on every connection) hit the index directly.
        builder.HasKey(x => new { x.UserId, x.GroupType, x.ResourceId });

        builder.Property(x => x.GroupType)
            .HasConversion<string>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
