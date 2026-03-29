using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.Models;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.UserProfileService.Persistence;

public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
    : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Email> Emails { get; set; }
    public DbSet<Phone> Phones { get; set; }
    public DbSet<EmailVerificationRequest> EmailVerificationRequests { get; set; }
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

