using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.IntegrationTests.Persistence;

public sealed class EntityBaseSaveChangesInterceptorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public EntityBaseSaveChangesInterceptorTests()
    {
        var services = new ServiceCollection();
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .AddInterceptors(sp.GetRequiredService<EntityBaseSaveChangesInterceptor>()));

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public async Task SavingChangesAsync_WhenEntityAdded_SetsCreatedByAndLastModifiedBy()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = UserProfile.Create(
            friendlyUserId: "interceptortest",
            displayName: "Interceptor Test User",
            emailAddress: EmailAddress.Create("interceptor@example.com"));

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        profile.CreatedBy.Should().Be("system");
        profile.LastModifiedBy.Should().Be("system");
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityAdded_SetsAuditTimestamps()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var before = DateTimeOffset.UtcNow;

        var profile = UserProfile.Create(
            friendlyUserId: "timestamptest",
            displayName: "Timestamp Test User",
            emailAddress: EmailAddress.Create("timestamp@example.com"));

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        profile.CreatedAtUtc.Should().BeOnOrAfter(before);
        profile.LastModifiedAtUtc.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityModified_UpdatesOnlyLastModifiedFields()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = UserProfile.Create(
            friendlyUserId: "modifytest",
            displayName: "Modify Test User",
            emailAddress: EmailAddress.Create("modify@example.com"));

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        var createdAt = profile.CreatedAtUtc;
        var createdBy = profile.CreatedBy;

        // Wait a tick so timestamps differ
        await Task.Delay(5);

        var email = profile.Emails[0];
        profile.SetMainEmail(email.Id);

        await db.SaveChangesAsync();

        // CreatedBy/CreatedAt must not change on modification
        profile.CreatedBy.Should().Be(createdBy);
        profile.CreatedAtUtc.Should().Be(createdAt);

        // LastModified fields must be updated
        profile.LastModifiedBy.Should().Be("system");
        profile.LastModifiedAtUtc.Should().BeOnOrAfter(createdAt);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenChildEntityAdded_SetsAuditFieldsOnChildToo()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var profile = UserProfile.Create(
            friendlyUserId: "childtest",
            displayName: "Child Test User",
            emailAddress: EmailAddress.Create("childtest@example.com"));

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        var initialEmail = profile.Emails[0];
        initialEmail.CreatedBy.Should().Be("system");
        initialEmail.LastModifiedBy.Should().Be("system");
    }
}
