using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FlowChat.UserProfileService.IntegrationTests.Persistence;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationSucceeds_CommitsChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<AppDbContext>(context);

        var profileId = await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var profile = UserProfile.Create(
                    Id<UserProfile>.New(),
                    "jdoe",
                    EmailAddress.Create("john@example.com"));

                await context.UserProfiles.AddAsync(profile, token);
                return profile.Id.Value;
            },
            CancellationToken.None);

        var persistedProfile = (await context.UserProfiles.ToListAsync()).Single(profile => profile.Id.Value == profileId);
        persistedProfile.FriendlyUserId.Value.Should().Be("jdoe");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBackTransaction()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<AppDbContext>(context);

        var act = () => unitOfWork.ExecuteInTransactionAsync<int>(
            async token =>
            {
                await context.UserProfiles.AddAsync(
                    UserProfile.Create(Id<UserProfile>.New(), "jdoe", EmailAddress.Create("john@example.com")),
                    token);

                throw new InvalidOperationException("boom");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        (await context.UserProfiles.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityWasAdded_PersistsChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<AppDbContext>(context);

        context.UserProfiles.Add(UserProfile.Create(Id<UserProfile>.New(), "jdoe", EmailAddress.Create("john@example.com")));

        var affectedRows = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        affectedRows.Should().BeGreaterThan(0);
        (await context.UserProfiles.CountAsync()).Should().Be(1);
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.SavingChanges += (_, _) => SetAuditFields(context);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SetAuditFields(AppDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>()
                     .Where(entry => entry.State == EntityState.Added && entry.Entity.CreatedAtUtc is null))
        {
            entry.Entity.SetCreated("test");
            entry.Entity.SetUpdated("test");
        }
    }
}
