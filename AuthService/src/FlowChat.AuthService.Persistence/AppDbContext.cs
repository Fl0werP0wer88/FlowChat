using FlowChat.AuthService.Persistence.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace FlowChat.AuthService.Persistence;

public class AppDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.MapWolverineEnvelopeStorage();
        modelBuilder.Entity("Wolverine.EntityFrameworkCore.Internals.IncomingMessage")
            .ToTable("wolverine_incoming_envelopes", null, table => table.ExcludeFromMigrations(false));
        modelBuilder.Entity("Wolverine.EntityFrameworkCore.Internals.OutgoingMessage")
            .ToTable("wolverine_outgoing_envelopes", null, table => table.ExcludeFromMigrations(false));
    }
}
