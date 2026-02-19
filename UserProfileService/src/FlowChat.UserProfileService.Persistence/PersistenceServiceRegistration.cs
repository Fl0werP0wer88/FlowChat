using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UserProfileDb")));

        services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();

        return services;
    }
}
