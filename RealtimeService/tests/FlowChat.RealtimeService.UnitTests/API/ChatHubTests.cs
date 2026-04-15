using System.Reflection;
using System.Security.Claims;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
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
            new TestHubCallerContext("connection-1", new ClaimsPrincipal(new ClaimsIdentity())),
            new CapturingGroupManager());

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeTrue();
        mediator.LastSentRequest.Should().BeNull();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserIdPresent_AddsGroupAndRegistersConnection()
    {
        var userId = Guid.NewGuid();
        var mediator = new CapturingMediator();
        var groups = new CapturingGroupManager();
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-1", CreatePrincipal(userId)), groups);

        await hub.OnConnectedAsync();

        groups.AddedConnections.Should().ContainSingle()
            .Which.Should().Be(("connection-1", GroupNames.ForUser(userId)));
        mediator.LastSentRequest.Should().BeOfType<RegisterRealtimeConnectionCommand>()
            .Which.Should().Be(new RegisterRealtimeConnectionCommand(userId, "connection-1"));
        GetContext(hub).AbortCalled.Should().BeFalse();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserIdPresent_FetchesContactPresenceStatuses()
    {
        var userId = Guid.NewGuid();
        var presenceClientMock = new Mock<IPresenceInternalApiClient>();
        presenceClientMock
            .Setup(x => x.GetContactPresenceStatusesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var hub = CreateHub(
            new CapturingMediator(),
            new TestHubCallerContext("connection-1", CreatePrincipal(userId)),
            new CapturingGroupManager(),
            presenceClientMock);

        await hub.OnConnectedAsync();

        presenceClientMock.Verify(
            x => x.GetContactPresenceStatusesAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenPresenceServiceFails_DoesNotAbortConnection()
    {
        var userId = Guid.NewGuid();
        var presenceClientMock = new Mock<IPresenceInternalApiClient>();
        presenceClientMock
            .Setup(x => x.GetContactPresenceStatusesAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("PresenceService unavailable"));

        var hub = CreateHub(
            new CapturingMediator(),
            new TestHubCallerContext("connection-1", CreatePrincipal(userId)),
            new CapturingGroupManager(),
            presenceClientMock);

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeFalse();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenRegisterCommandFails_AbortsConnectionAndRollsBackGroupMembership()
    {
        var userId = Guid.NewGuid();
        var mediator = new CapturingMediator
        {
            SendUnitResult = FlowChatResult<Unit>.Failure(DomainError.UnExpected("Failed to register realtime connection."))
        };
        var groups = new CapturingGroupManager();
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-1", CreatePrincipal(userId)), groups);

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeTrue();
        groups.RemovedConnections.Should().ContainSingle()
            .Which.Should().Be(("connection-1", GroupNames.ForUser(userId)));
    }

    [Fact]
    public async Task OnDisconnectedAsync_UnregistersConnectionByConnectionId()
    {
        var mediator = new CapturingMediator();
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-9"), new CapturingGroupManager());

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
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-9"), new CapturingGroupManager());

        var act = () => hub.OnDisconnectedAsync(null);

        await act.Should().NotThrowAsync();
        mediator.LastSentRequest.Should().BeOfType<UnregisterRealtimeConnectionCommand>()
            .Which.Should().Be(new UnregisterRealtimeConnectionCommand("connection-9"));
    }

    private static ChatHub CreateHub(
        IMediator mediator,
        TestHubCallerContext context,
        CapturingGroupManager groups,
        Mock<IPresenceInternalApiClient>? presenceClientMock = null)
    {
        var logger = new Mock<ILogger<ChatHub>>();

        if (presenceClientMock is null)
        {
            presenceClientMock = new Mock<IPresenceInternalApiClient>();
            presenceClientMock
                .Setup(x => x.GetContactPresenceStatusesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        }

        var hub = new ChatHub(logger.Object, mediator, presenceClientMock.Object);

        SetHubProperty(hub, nameof(Hub.Context), context);
        SetHubProperty(hub, nameof(Hub.Groups), groups);

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
