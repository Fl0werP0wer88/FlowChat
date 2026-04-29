using FlowChat.Shared.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Shared.Persistance;

public static class DbUpdateExceptionClassifierServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDbUpdateExceptionClassifier(
        this IServiceCollection services,
        Action<PostgresDbUpdateExceptionClassifierOptions>? configure = null)
    {
        var options = new PostgresDbUpdateExceptionClassifierOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IDbUpdateExceptionClassifier, PostgresDbUpdateExceptionClassifier>();

        return services;
    }
}
