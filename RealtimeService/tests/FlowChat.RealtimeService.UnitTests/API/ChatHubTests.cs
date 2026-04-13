using System.Reflection;
using System.Security.Claims;
using FlowChat.RealtimeService.Api.Realtime;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ChatHubTests
{
    [Fact]
    public async Task OnConnectedAsync_WhenUserIdMissing_AbortsConnectionAndDoesNotRegister()
    {
        var lifecycleService = new CapturingRealtimeConnectionLifecycleService();
        var hub = CreateHub(
            lifecycleService,
            new TestHubCallerContext("connection-1", new ClaimsPrincipal(new ClaimsIdentity())),
            new CapturingGroupManager());

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeTrue();
        lifecycleService.LastRegisteredConnectionId.Should().BeNull();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserIdPresent_AddsGroupAndRegistersConnection()
    {
        var userId = Guid.NewGuid();
        var lifecycleService = new CapturingRealtimeConnectionLifecycleService();
        var groups = new CapturingGroupManager();
        var hub = CreateHub(lifecycleService, new TestHubCallerContext("connection-1", CreatePrincipal(userId)), groups);

        await hub.OnConnectedAsync();

        groups.AddedConnections.Should().ContainSingle()
            .Which.Should().Be(("connection-1", GroupNames.ForUser(userId)));
        lifecycleService.LastRegisteredUserId.Should().Be(userId);
        lifecycleService.LastRegisteredConnectionId.Should().Be("connection-1");
        GetContext(hub).AbortCalled.Should().BeFalse();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenRegisterFails_AbortsConnectionAndRollsBackGroupMembership()
    {
        var userId = Guid.NewGuid();
        var lifecycleService = new CapturingRealtimeConnectionLifecycleService
        {
            RegisterException = new InvalidOperationException("redis unavailable")
        };
        var groups = new CapturingGroupManager();
        var hub = CreateHub(lifecycleService, new TestHubCallerContext("connection-1", CreatePrincipal(userId)), groups);

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeTrue();
        groups.RemovedConnections.Should().ContainSingle()
            .Which.Should().Be(("connection-1", GroupNames.ForUser(userId)));
    }

    [Fact]
    public async Task OnDisconnectedAsync_UnregistersConnectionByConnectionId()
    {
        var lifecycleService = new CapturingRealtimeConnectionLifecycleService();
        var hub = CreateHub(lifecycleService, new TestHubCallerContext("connection-9"), new CapturingGroupManager());

        await hub.OnDisconnectedAsync(null);

        lifecycleService.LastUnregisteredConnectionId.Should().Be("connection-9");
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenUnregisterFails_DoesNotThrow()
    {
        var lifecycleService = new CapturingRealtimeConnectionLifecycleService
        {
            UnregisterException = new InvalidOperationException("redis unavailable")
        };
        var hub = CreateHub(lifecycleService, new TestHubCallerContext("connection-9"), new CapturingGroupManager());

        var act = () => hub.OnDisconnectedAsync(null);

        await act.Should().NotThrowAsync();
        lifecycleService.LastUnregisteredConnectionId.Should().Be("connection-9");
    }

    private static ChatHub CreateHub(
        CapturingRealtimeConnectionLifecycleService lifecycleService,
        TestHubCallerContext context,
        CapturingGroupManager groups)
    {
        var logger = new Mock<ILogger<ChatHub>>();
        var hub = new ChatHub(logger.Object, lifecycleService);

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
