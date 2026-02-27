using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.Configurations;

public sealed class InvitationConfiguration : IEntityTypeConfiguration<InvitationEntity>
{
    public void Configure(EntityTypeBuilder<InvitationEntity> builder)
    {
        builder.ToTable("Invitations", t =>
            t.HasCheckConstraint("chk_invitation_different_users", "\"RequesterId\" <> \"AddresseeId\""));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => new { x.RequesterId, x.AddresseeId, x.Status })
            .HasDatabaseName("ix_invitations_requester_addressee_status");
    }
}
