using FlowChat.HarnessService.API.Features.Projections.Internal.BulkUpsert;
using FlowChat.HarnessService.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.AATs.Infrastructure;

// Anchored to BulkUpsertProjectionController to avoid Program ambiguity (API + Consumers both define Program)
public sealed class HarnessApiFactory : WebApplicationFactory<BulkUpsertProjectionController>
{
    private readonly string _connectionString;

    public HarnessApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            var dbContextFactoryDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextFactory<AppDbContext>));
            if (dbContextFactoryDescriptor is not null)
                services.Remove(dbContextFactoryDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_connectionString));
            services.AddDbContextFactory<AppDbContext>(
                options => options.UseNpgsql(_connectionString),
                ServiceLifetime.Scoped);
        });
    }
}
