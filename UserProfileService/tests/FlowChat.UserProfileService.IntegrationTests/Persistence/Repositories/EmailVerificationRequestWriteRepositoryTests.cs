using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
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
                Id<UserProfile>.New(),
                "jdoe",
                EmailAddress.Create("john@example.com"));
            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            emailId = profile.Emails.Single().Id;

            var activeRequest = EmailVerificationRequest.Create(
                Id<EmailVerificationRequest>.New(),
                profile.Id,
                emailId,
                "active-nonce",
                DateTimeOffset.UtcNow.AddHours(2));
            var expiredRequest = EmailVerificationRequest.Create(
                Id<EmailVerificationRequest>.New(),
                profile.Id,
                emailId,
                "expired-nonce",
                DateTimeOffset.UtcNow.AddHours(-2));
            var invalidatedRequest = EmailVerificationRequest.Create(
                Id<EmailVerificationRequest>.New(),
                profile.Id,
                emailId,
                "invalidated-nonce",
                DateTimeOffset.UtcNow.AddHours(2));
            invalidatedRequest.Invalidate(DateTimeOffset.UtcNow);
            var consumedRequest = EmailVerificationRequest.Create(
                Id<EmailVerificationRequest>.New(),
                profile.Id,
                emailId,
                "consumed-nonce",
                DateTimeOffset.UtcNow.AddHours(2));
            consumedRequest.Consume(DateTimeOffset.UtcNow);

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
                Id<UserProfile>.New(),
                "jdoe",
                EmailAddress.Create("john@example.com"));
            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            var request = EmailVerificationRequest.Create(
                Id<EmailVerificationRequest>.New(),
                profile.Id,
                profile.Emails.Single().Id,
                "nonce-123",
                DateTimeOffset.UtcNow.AddHours(1));

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
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
