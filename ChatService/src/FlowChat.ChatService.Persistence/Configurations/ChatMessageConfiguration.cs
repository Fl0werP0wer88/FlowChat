using FlowChat.ChatService.Domain.Entities;
using FlowChat.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowChat.ChatService.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => Id<ChatMessage>.FromGuid(x));

        builder.Property(x => x.ConversationId)
            .IsRequired();

        builder.Property(x => x.SenderUserId)
            .IsRequired();

        builder.Property(x => x.SenderDisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Text)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.SentAtUtc)
            .IsRequired();

        builder.Property(x => x.RecipientUserIds)
            .HasColumnType("uuid[]")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("CreatedDate");

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LastModifiedAtUtc)
            .HasColumnName("LastModifiedDate");

        builder.HasIndex(x => new { x.ConversationId, x.SentAtUtc });
    }
}
