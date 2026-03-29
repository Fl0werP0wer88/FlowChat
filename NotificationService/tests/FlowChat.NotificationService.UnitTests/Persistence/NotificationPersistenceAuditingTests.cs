using FlowChat.NotificationService.Persistence;
using FlowChat.Shared.Persistance.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.NotificationService.UnitTests.Persistence;

public sealed class NotificationPersistenceAuditingTests
{
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

        var interceptors = Assert.IsAssignableFrom<IEnumerable<IInterceptor>>(coreOptionsExtension.Interceptors);

        Assert.Contains(interceptors, interceptor => interceptor is EntityBaseSaveChangesInterceptor);
    }

    [Fact]
    public async Task AppDbContext_SaveChanges_UsesEntityBaseAuditInterceptor()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        await using var dbContext = new AppDbContext(options);
        var notification = FlowChat.NotificationService.Domain.Entities.Notification.CreateEmailVerification(
            Guid.NewGuid(),
            "test@example.com",
            "Test User",
            sourceMessageKey: "source-key");

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        var createdAtUtc = notification.CreatedAtUtc;

        Assert.Equal("system", notification.CreatedBy);
        Assert.Equal("system", notification.LastModifiedBy);
        Assert.Equal(createdAtUtc, notification.LastModifiedAtUtc);

        notification.MarkFailed("failure");
        await dbContext.SaveChangesAsync();

        Assert.Equal("system", notification.CreatedBy);
        Assert.Equal(createdAtUtc, notification.CreatedAtUtc);
        Assert.Equal("system", notification.LastModifiedBy);
        Assert.True(notification.LastModifiedAtUtc >= createdAtUtc);
    }
}

