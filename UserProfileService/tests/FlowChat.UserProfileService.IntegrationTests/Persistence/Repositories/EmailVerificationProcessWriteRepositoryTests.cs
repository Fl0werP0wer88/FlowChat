using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.IntegrationTests.Persistence.Repositories;

public sealed class EmailVerificationProcessWriteRepositoryTests
{
    [Fact]
    public async Task GetByEmailIdAsync_WhenProcessExists_ReturnsProcessWithRequests()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Id<Email> emailId;
        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                Id<UserProfile>.New(),
                "jdoe",
                EmailAddress.Create("john@example.com"));
            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            emailId = profile.Emails.Single().Id;
            var process = EmailVerificationProcess.Create(profile.Id, emailId);
            process.IssueRequest(
                Id<EmailVerificationRequest>.New(),
                "nonce-123",
                DateTimeOffset.UtcNow.AddHours(2),
                DateTimeOffset.UtcNow);

            seedContext.EmailVerificationProcesses.Add(process);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new EmailVerificationProcessWriteRepository(readContext);

        var result = await repository.GetByEmailIdAsync(emailId.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.EmailId.Should().Be(emailId);
        result.Requests.Should().ContainSingle().Which.Nonce.Should().Be("nonce-123");
    }

    [Fact]
    public async Task GetByNonceAsync_WhenRequestExists_ReturnsProcessWithMatchingRequest()
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

            var process = EmailVerificationProcess.Create(profile.Id, profile.Emails.Single().Id);
            process.IssueRequest(
                Id<EmailVerificationRequest>.New(),
                "nonce-123",
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow);

            seedContext.EmailVerificationProcesses.Add(process);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new EmailVerificationProcessWriteRepository(readContext);

        var result = await repository.GetByNonceAsync("nonce-123", CancellationToken.None);

        result.Should().NotBeNull();
        result!.TryGetRequestByNonce("nonce-123", out var request).Should().BeTrue();
        request.Nonce.Should().Be("nonce-123");
    }

    [Fact]
    public async Task GetConfirmationStateByNonceAsync_WhenRequestExists_ReturnsRequestAndEmailState()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Id<UserProfile> profileId;
        Id<Email> emailId;
        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                Id<UserProfile>.New(),
                "jdoe",
                EmailAddress.Create("john@example.com"));
            var email = profile.Emails.Single();
            profile.ConfirmEmail(email.Id);
            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            profileId = profile.Id;
            emailId = email.Id;
            var process = EmailVerificationProcess.Create(profile.Id, email.Id);
            var request = process.IssueRequest(
                Id<EmailVerificationRequest>.New(),
                "nonce-123",
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow);
            process.ConsumeRequest(request.Nonce, DateTimeOffset.UtcNow);

            seedContext.EmailVerificationProcesses.Add(process);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new EmailVerificationProcessWriteRepository(readContext);

        var result = await repository.GetConfirmationStateByNonceAsync("nonce-123", CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserProfileId.Should().Be(profileId.Value);
        result.EmailId.Should().Be(emailId.Value);
        result.ConsumedAtUtc.Should().NotBeNull();
        result.EmailIsConfirmed.Should().BeTrue();
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
