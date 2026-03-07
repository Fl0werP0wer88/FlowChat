using System.Data.Common;
using FlowChat.AuthService.Persistence.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.AuthService.Persistence;

public class AppDbContext : IdentityDbContext<UserEntity, RoleEntity, Guid>
{
    [ActivatorUtilitiesConstructor]
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbConnection connection)
        : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options)
    {
    }

    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
