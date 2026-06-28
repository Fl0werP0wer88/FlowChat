using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class WorkerApplicationServiceRegistrationTests
{
    [Fact]
    public void AddWorkerApplicationServices_DoesNotRequireRealtimeClientDispatcher()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddScoped(_ => Mock.Of<IRealtimeEventRouter>());
        services.AddScoped(_ => Mock.Of<IChatServiceInternalApiClient>());
        services.AddWorkerApplicationServices();

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        serviceProvider.Should().NotBeNull();
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<RouteMessageCommand, FlowChatResult<Unit>>>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IRealtimeClientDispatcher>().Should().BeNull();
    }
}
