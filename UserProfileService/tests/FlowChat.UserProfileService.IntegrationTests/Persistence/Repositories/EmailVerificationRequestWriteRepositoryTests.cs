using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.IntegrationTests.Persistence.Repositories;

public sealed class EmailVerificationRequestWriteRepositoryTests
{
    [Fact]
    public async Task GetActiveByEmailIdAsync_ReturnsOnlyNonExpiredNonInvalidatedNonConsumedRequests()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var emailId = Id<Email>.New();
        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                "jdoe",
                "John Doe",
                EmailAddress.Create("john@example.com"),
                id: Id<UserProfile>.New());
            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            emailId = profile.Emails.Single().Id;

            var activeRequest = EmailVerificationRequest.Create(
                profile.Id,
                emailId,
                "active-nonce",
                DateTime.UtcNow.AddHours(2));
            var expiredRequest = EmailVerificationRequest.Create(
                profile.Id,
                emailId,
                "expired-nonce",
                DateTime.UtcNow.AddHours(-2));
            var invalidatedRequest = EmailVerificationRequest.Create(
                profile.Id,
                emailId,
                "invalidated-nonce",
                DateTime.UtcNow.AddHours(2));
            invalidatedRequest.Invalidate(DateTime.UtcNow);
            var consumedRequest = EmailVerificationRequest.Create(
                profile.Id,
                emailId,
                "consumed-nonce",
                DateTime.UtcNow.AddHours(2));
            consumedRequest.Consume(DateTime.UtcNow);

            seedContext.EmailVerificationRequests.AddRange(activeRequest, expiredRequest, invalidatedRequest, consumedRequest);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new EmailVerificationRequestWriteRepository(readContext);

        var result = await repository.GetActiveByEmailIdAsync(emailId.Value, CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().Nonce.Should().Be("active-nonce");
    }

    [Fact]
    public async Task GetByNonceAsync_WhenRequestExists_ReturnsMatchingRequest()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                "jdoe",
                "John Doe",
                EmailAddress.Create("john@example.com"));
            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            var request = EmailVerificationRequest.Create(
                profile.Id,
                profile.Emails.Single().Id,
                "nonce-123",
                DateTime.UtcNow.AddHours(1));

            seedContext.EmailVerificationRequests.Add(request);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new EmailVerificationRequestWriteRepository(readContext);

        var result = await repository.GetByNonceAsync("nonce-123", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Nonce.Should().Be("nonce-123");
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
