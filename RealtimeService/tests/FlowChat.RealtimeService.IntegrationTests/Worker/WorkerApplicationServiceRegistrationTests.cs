using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsAdded;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsRemoved;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class WorkerApplicationServiceRegistrationTests
{
    [Fact]
    public void AddApiApplicationServices_DoesNotRegisterConsumerRouteHandlers()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddScoped(_ => Mock.Of<IRealtimeClientDispatcher>());
        services.AddScoped(_ => Mock.Of<IRealtimeConnectionRegistry>());
        services.AddScoped(_ => Mock.Of<IRealtimeGroupManager>());
        services.AddScoped(_ => Mock.Of<IPresenceInternalApiClient>());
        services.AddScoped(_ => Mock.Of<IRealtimeGroupMembershipReadModelRepository>());
        services.AddScoped(_ => Mock.Of<IRealtimeGroupMembershipRevisionTrackerRepository>());
        services.AddApiApplicationServices();

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<PublishMessageCommand, FlowChatResult<Unit>>>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IRequestHandler<RouteMessageCommand, FlowChatResult<Unit>>>().Should().BeNull();
        scope.ServiceProvider.GetService<IRequestHandler<RoutePresenceChangeCommand, FlowChatResult<Unit>>>().Should().BeNull();
        scope.ServiceProvider.GetService<IRequestHandler<RouteGroupConversationChangedCommand, FlowChatResult<Unit>>>().Should().BeNull();
        scope.ServiceProvider.GetService<IRequestHandler<RouteGroupConversationParticipantsAddedCommand, FlowChatResult<Unit>>>().Should().BeNull();
        scope.ServiceProvider.GetService<IRequestHandler<RouteGroupConversationParticipantsRemovedCommand, FlowChatResult<Unit>>>().Should().BeNull();
        scope.ServiceProvider.GetService<IRealtimeEventRouter>().Should().BeNull();
    }

    [Fact]
    public void AddConsumerApplicationServices_RegistersConsumerRouteHandlers()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddScoped(_ => Mock.Of<IRealtimeEventRouter>());
        services.AddScoped(_ => Mock.Of<IChatServiceInternalApiClient>());
        services.AddScoped(_ => Mock.Of<IRealtimeGroupMembershipReadModelRepository>());
        services.AddScoped(_ => Mock.Of<IRealtimeGroupMembershipRevisionTrackerRepository>());
        services.AddScoped(_ => Mock.Of<IUnitOfWork>());
        services.AddConsumerApplicationServices();

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMediator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<RouteMessageCommand, FlowChatResult<Unit>>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<RoutePresenceChangeCommand, FlowChatResult<Unit>>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<RouteGroupConversationChangedCommand, FlowChatResult<Unit>>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<RouteGroupConversationParticipantsAddedCommand, FlowChatResult<Unit>>>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IRequestHandler<RouteGroupConversationParticipantsRemovedCommand, FlowChatResult<Unit>>>().Should().NotBeNull();
        scope.ServiceProvider.GetService<IRequestHandler<PublishMessageCommand, FlowChatResult<Unit>>>().Should().BeNull();
        scope.ServiceProvider.GetService<IRealtimeClientDispatcher>().Should().BeNull();
    }
}
