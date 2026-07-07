using System.Reflection;
using System.Security.Claims;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;
using FlowChat.RealtimeService.Domain.Enums;
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
    public async Task OnConnectedAsync_WhenUserHasConversationMemberships_JoinsConversationGroups()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var groups = new CapturingGroupManager();
        var membershipRepositoryMock = new Mock<IRealtimeGroupMembershipRepository>();
        membershipRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RealtimeGroupMembershipDto>)
            [
                new RealtimeGroupMembershipDto(userId, RealtimeGroupType.Conversation, conversationId, DateTimeOffset.UtcNow)
            ]);

        var hub = CreateHub(
            new CapturingMediator(),
            new TestHubCallerContext("connection-1", CreatePrincipal(userId)),
            groups,
            realtimeGroupMembershipRepositoryMock: membershipRepositoryMock);

        await hub.OnConnectedAsync();

        groups.AddedConnections.Should().Contain(("connection-1", GroupNames.ForConversation(conversationId)));
    }

    [Fact]
    public async Task OnConnectedAsync_WhenUserIdPresent_InitializesPresenceStatus()
    {
        var userId = Guid.NewGuid();
        var presenceClientMock = new Mock<IPresenceInternalApiClient>();
        presenceClientMock
            .Setup(x => x.InitializePresenceStatusAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = CreateHub(
            new CapturingMediator(),
            new TestHubCallerContext("connection-1", CreatePrincipal(userId)),
            new CapturingGroupManager(),
            presenceClientMock);

        await hub.OnConnectedAsync();

        presenceClientMock.Verify(
            x => x.InitializePresenceStatusAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenPresenceInitializationFails_AbortsConnectionAndUnregisters()
    {
        var userId = Guid.NewGuid();
        var mediator = new CapturingMediator();
        var groups = new CapturingGroupManager();
        var presenceClientMock = new Mock<IPresenceInternalApiClient>();
        presenceClientMock
            .Setup(x => x.InitializePresenceStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("PresenceService unavailable"));

        var hub = CreateHub(
            mediator,
            new TestHubCallerContext("connection-1", CreatePrincipal(userId)),
            groups,
            presenceClientMock);

        await hub.OnConnectedAsync();

        GetContext(hub).AbortCalled.Should().BeTrue();
        groups.RemovedConnections.Should().ContainSingle()
            .Which.Should().Be(("connection-1", GroupNames.ForUser(userId)));
        mediator.SentRequests.OfType<UnregisterRealtimeConnectionCommand>()
            .Should().ContainSingle()
            .Which.ConnectionId.Should().Be("connection-1");
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

    [Fact]
    public async Task OnDisconnectedAsync_WhenMediatorThrows_PropagatesException()
    {
        var mediator = new CapturingMediator
        {
            SendException = new InvalidOperationException("Unregister handler resolution failed.")
        };
        var hub = CreateHub(mediator, new TestHubCallerContext("connection-9"), new CapturingGroupManager());

        var act = () => hub.OnDisconnectedAsync(null);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static ChatHub CreateHub(
        IMediator mediator,
        TestHubCallerContext context,
        CapturingGroupManager groups,
        Mock<IPresenceInternalApiClient>? presenceClientMock = null,
        Mock<IRealtimeGroupMembershipRepository>? realtimeGroupMembershipRepositoryMock = null)
    {
        var logger = new Mock<ILogger<ChatHub>>();

        if (presenceClientMock is null)
        {
            presenceClientMock = new Mock<IPresenceInternalApiClient>();
            presenceClientMock
                .Setup(x => x.InitializePresenceStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        if (realtimeGroupMembershipRepositoryMock is null)
        {
            realtimeGroupMembershipRepositoryMock = new Mock<IRealtimeGroupMembershipRepository>();
            realtimeGroupMembershipRepositoryMock
                .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<RealtimeGroupMembershipDto>)[]);
        }

        var hub = new ChatHub(logger.Object, mediator, presenceClientMock.Object, realtimeGroupMembershipRepositoryMock.Object);

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
