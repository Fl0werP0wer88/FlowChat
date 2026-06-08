using FlowChat.HarnessService.Persistence.Entities.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.HarnessService.Persistence.Configuration.Entities.Projections;

public sealed class ProjectionTestEntityConfiguration : IEntityTypeConfiguration<ProjectionTestEntity>
{
    public void Configure(EntityTypeBuilder<ProjectionTestEntity> builder)
    {
        builder.ToTable("ProjectionTests");
        builder.HasKey(x => x.Id);
        // varchar(100) constraint is the deterministic trigger for IsolableException in bulk upsert tests
        builder.Property(x => x.Payload).HasMaxLength(100).IsRequired();
    }
}
