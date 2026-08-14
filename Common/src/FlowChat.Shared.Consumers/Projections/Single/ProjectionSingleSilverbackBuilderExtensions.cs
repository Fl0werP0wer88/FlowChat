using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

namespace FlowChat.Shared.Consumers.Projections.Single;

public static class ProjectionSingleSilverbackBuilderExtensions
{
    public static SilverbackBuilder AddProjectionSingle<TReadModel, TValue, TRepository, TSubscriber>(
        this SilverbackBuilder builder)
        where TReadModel : class
        where TValue : class
        where TRepository : class, IProjectionSingleRepository<TValue>
        where TSubscriber : ProjectionSingleSubscriberBase<TReadModel, TValue>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IProjectionSingleRepository<TValue>, TRepository>();
        builder.Services.AddScoped<
            IRequestHandler<ProjectionSingleCommand<TValue>, FlowChat.Core.Results.FlowChatResult<Unit>>,
            ProjectionSingleCommandHandler<TValue>>();

        return builder.AddScopedSubscriber<TSubscriber>();
    }
}
