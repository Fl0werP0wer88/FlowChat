using System.Data.Common;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.UserProfileService.Persistence;

public class AppDbContext : DbContext, IDataProtectionKeyContext
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

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Email> Emails { get; set; }
    public DbSet<Phone> Phones { get; set; }
    public DbSet<EmailVerificationProcess> EmailVerificationProcesses { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<SilverbackOutboxMessage> SilverbackOutboxMessages => Set<SilverbackOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Setting default schema for tables creation.
        // modelBuilder.HasDefaultSchema("FlowChat");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys");
        base.OnModelCreating(modelBuilder);
    }
}

