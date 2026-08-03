using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

namespace FlowChat.Shared.Consumers.Projections.Single;

public static class ProjectionSingleSilverbackBuilderExtensions
{
    public static SilverbackBuilder AddProjectionSingle<TReadModel, TValue, TValueFactory, TRepository>(
        this SilverbackBuilder builder)
        where TReadModel : class
        where TValue : class
        where TValueFactory : class, IProjectionSingleValueFactory<TReadModel, TValue>
        where TRepository : class, IProjectionSingleRepository<TValue>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IProjectionSingleValueFactory<TReadModel, TValue>, TValueFactory>();
        builder.Services.AddScoped<IProjectionSingleRepository<TValue>, TRepository>();
        builder.Services.AddScoped<
            IRequestHandler<ProjectionSingleCommand<TValue>, FlowChat.Core.Results.FlowChatResult<Unit>>,
            ProjectionSingleCommandHandler<TValue>>();

        return builder.AddScopedSubscriber<ProjectionSingleSubscriber<TReadModel, TValue>>();
    }
}
