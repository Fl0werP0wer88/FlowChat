using FlowChat.NotificationService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.NotificationService.Persistence.Configuration.Entities;

public sealed class NotificationReadEntityConfiguration : IEntityTypeConfiguration<NotificationReadEntity>
{
    public void Configure(EntityTypeBuilder<NotificationReadEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToView("Notifications");

        builder.Property(x => x.Type)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();
    }
}
