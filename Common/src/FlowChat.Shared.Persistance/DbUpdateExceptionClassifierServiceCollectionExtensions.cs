using FlowChat.Shared.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Shared.Persistance;

public static class DbUpdateExceptionClassifierServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDbUpdateExceptionClassifier(this IServiceCollection services)
    {
        services.AddSingleton<IDbUpdateExceptionClassifier, PostgresDbUpdateExceptionClassifier>();

        return services;
    }
}
