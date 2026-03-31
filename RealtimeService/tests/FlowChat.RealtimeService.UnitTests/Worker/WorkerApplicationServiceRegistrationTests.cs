using FlowChat.RealtimeService.Application;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class WorkerApplicationServiceRegistrationTests
{
    [Fact]
    public void AddWorkerApplicationServices_DoesNotRequireRealtimeClientDispatcher()
    {
        var services = new ServiceCollection();

        services.AddWorkerApplicationServices();

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        serviceProvider.Should().NotBeNull();
    }
}
