using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.SocialGraphService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var persistenceAssembly = typeof(PersistenceServiceRegistration).Assembly;

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("SocialGraphDb")));
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, persistenceAssembly);

        services.AddScoped<IUserSocialGraphRepository, UserSocialGraphRepository>();
        services.AddScoped<IContactReadRepository, ContactReadRepository>();
        services.AddScoped<IInvitationReadRepository, InvitationReadRepository>();

        return services;
    }
}
