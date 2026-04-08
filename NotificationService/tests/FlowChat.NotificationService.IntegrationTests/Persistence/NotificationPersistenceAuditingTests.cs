using AutoFixture;
using FlowChat.NotificationService.Persistence;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.NotificationService.UnitTests.Persistence;

public sealed class NotificationPersistenceAuditingTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public void AddPersistenceServices_ConfiguresEntityBaseSaveChangesInterceptor()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("ConnectionStrings:NotificationDb", "Host=localhost;Database=test;Username=test;Password=test")
            ])
            .Build();

        services.AddPersistenceServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        var coreOptionsExtension = options.Extensions.OfType<CoreOptionsExtension>().Single();
        var interceptors = coreOptionsExtension.Interceptors.Should().BeAssignableTo<IEnumerable<IInterceptor>>().Subject;

        interceptors.Should().Contain(interceptor => interceptor is EntityBaseSaveChangesInterceptor);
    }

    [Fact]
    public async Task AppDbContext_SaveChanges_UsesEntityBaseAuditInterceptor()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        await using var dbContext = new AppDbContext(options);
        var notification = FlowChat.NotificationService.Domain.Entities.Notification.Notification.CreateEmailVerification(
            _fixture.Create<Guid>(),
            EmailAddress.Create("test@example.com"),
            "Test User",
            "Confirm your email by clicking the provided link",
            sourceMessageKey: "source-key");

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        var createdAtUtc = notification.CreatedAtUtc;

        notification.CreatedBy.Should().Be("system");
        notification.LastModifiedBy.Should().Be("system");
        notification.LastModifiedAtUtc.Value.Should().BeOnOrAfter(createdAtUtc.Value);

        notification.MarkFailed("failure");
        await dbContext.SaveChangesAsync();

        notification.CreatedBy.Should().Be("system");
        notification.CreatedAtUtc.Should().Be(createdAtUtc);
        notification.LastModifiedBy.Should().Be("system");
        notification.LastModifiedAtUtc.Value.Should().BeOnOrAfter(createdAtUtc.Value);
    }
}
