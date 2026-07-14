using System.Reflection;
using System.Security.Claims;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ChatHubTests
{
    [Fact]
    public async Task OnConnectedAsync_WhenUserIdMissing_AbortsConnectionAndDoesNotRegister()
    {
        var mediator = new CapturingMediator();
        var hub = CreateHub(
            mediator,
            new TestHubCallerContext("connection-1", new ClaimsPrincipal(new ClaimsIdentity())));

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeTrue();
        mediator.LastSentRequest.Should().BeNull();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserIdPresent_RegistersConnection()
    {
        var userId = Guid.NewGuid();
        var mediator = new CapturingMediator();
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-1", CreatePrincipal(userId)));

        await hub.OnConnectedAsync();

        mediator.LastSentRequest.Should().BeOfType<RegisterRealtimeConnectionCommand>()
            .Which.Should().Be(new RegisterRealtimeConnectionCommand(userId, "connection-1"));
        GetContext(hub).AbortCalled.Should().BeFalse();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenRegisterCommandFails_AbortsConnection()
    {
        var userId = Guid.NewGuid();
        var mediator = new CapturingMediator
        {
            SendUnitResult = FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to register realtime connection."))
        };
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-1", CreatePrincipal(userId)));

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnDisconnectedAsync_UnregistersConnectionByConnectionId()
    {
        var mediator = new CapturingMediator();
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-9"));

        await hub.OnDisconnectedAsync(null);

        mediator.LastSentRequest.Should().BeOfType<UnregisterRealtimeConnectionCommand>()
            .Which.Should().Be(new UnregisterRealtimeConnectionCommand("connection-9"));
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenUnregisterCommandFails_DoesNotThrow()
    {
        var mediator = new CapturingMediator
        {
            SendUnitResult = FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to unregister realtime connection."))
        };
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-9"));

        var act = () => hub.OnDisconnectedAsync(null);

        await act.Should().NotThrowAsync();
        mediator.LastSentRequest.Should().BeOfType<UnregisterRealtimeConnectionCommand>()
            .Which.Should().Be(new UnregisterRealtimeConnectionCommand("connection-9"));
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenMediatorThrows_PropagatesException()
    {
        var mediator = new CapturingMediator
        {
            SendException = new InvalidOperationException("Unregister handler resolution failed.")
        };
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-9"));

        var act = () => hub.OnDisconnectedAsync(null);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static ChatHub CreateHub(IMediator mediator, TestHubCallerContext context)
    {
        var logger = new Mock<ILogger<ChatHub>>();

        var hub = new ChatHub(logger.Object, mediator);

        SetHubProperty(hub, nameof(Hub.Context), context);

        return hub;
    }

    private static TestHubCallerContext GetContext(ChatHub hub) => (TestHubCallerContext)hub.Context;

    private static ClaimsPrincipal CreatePrincipal(Guid userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString())], "test"));

    private static void SetHubProperty(object hub, string propertyName, object value)
    {
        var property = typeof(Hub).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property.Should().NotBeNull();
        property!.SetValue(hub, value);
    }
}
