using FlowChat.HarnessService.Persistence.Entities.Retry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.HarnessService.Persistence.Configuration.Entities.Retry;

public sealed class RetryPipelineTestResultEntityConfiguration
    : IEntityTypeConfiguration<RetryPipelineTestResultEntity>
{
    public void Configure(EntityTypeBuilder<RetryPipelineTestResultEntity> builder)
    {
        builder.ToTable("RetryPipelineTestResults");
        builder.HasKey(x => x.ScenarioId);
    }
}
