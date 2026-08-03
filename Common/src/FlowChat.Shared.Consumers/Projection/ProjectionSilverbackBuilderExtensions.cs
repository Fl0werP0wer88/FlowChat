using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

namespace FlowChat.Shared.Consumers.Projection;

public static class ProjectionSilverbackBuilderExtensions
{
    public static SilverbackBuilder AddProjection<TReadModel, TValue, TValueFactory, TRepository>(
        this SilverbackBuilder builder)
        where TReadModel : class
        where TValue : class
        where TValueFactory : class, IProjectionValueFactory<TReadModel, TValue>
        where TRepository : class, IProjectionRepository<TValue>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IProjectionValueFactory<TReadModel, TValue>, TValueFactory>();
        builder.Services.AddScoped<IProjectionRepository<TValue>, TRepository>();
        builder.Services.AddScoped<
            IRequestHandler<ProjectionCommand<TValue>, FlowChat.Core.Results.FlowChatResult<Unit>>,
            ProjectionCommandHandler<TValue>>();

        return builder.AddScopedSubscriber<ProjectionSubscriber<TReadModel, TValue>>();
    }
}
