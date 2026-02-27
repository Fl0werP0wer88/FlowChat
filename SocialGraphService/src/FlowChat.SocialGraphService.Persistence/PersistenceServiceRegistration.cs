using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
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
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("SocialGraphDb")));

        services.AddScoped<IAsyncRepository<Contact>, ContactRepository>();
        services.AddScoped<IAsyncRepository<Invitation>, InvitationRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();

        return services;
    }
}
