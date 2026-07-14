using FlowChat.RealtimeService.Api.Realtime;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class SignalRRealtimeGroupManagerTests
{
    [Fact]
    public async Task AddToUserGroupAsync_AddsConnectionToUserGroup()
    {
        var userId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Groups).Returns(groupsMock.Object);
        var groupManager = new SignalRRealtimeGroupManager(hubContextMock.Object);

        await groupManager.AddToUserGroupAsync("connection-1", userId, CancellationToken.None);

        groupsMock.Verify(
            x => x.AddToGroupAsync("connection-1", GroupNames.ForUser(userId), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task RemoveFromUserGroupAsync_RemovesConnectionFromUserGroup()
    {
        var userId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Groups).Returns(groupsMock.Object);
        var groupManager = new SignalRRealtimeGroupManager(hubContextMock.Object);

        await groupManager.RemoveFromUserGroupAsync("connection-1", userId, CancellationToken.None);

        groupsMock.Verify(
            x => x.RemoveFromGroupAsync("connection-1", GroupNames.ForUser(userId), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task AddToConversationGroupAsync_AddsConnectionToConversationGroup()
    {
        var conversationId = Guid.NewGuid();
        var groupsMock = new Mock<IGroupManager>();
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Groups).Returns(groupsMock.Object);
        var groupManager = new SignalRRealtimeGroupManager(hubContextMock.Object);

        await groupManager.AddToConversationGroupAsync("connection-1", conversationId, CancellationToken.None);

        groupsMock.Verify(
            x => x.AddToGroupAsync("connection-1", GroupNames.ForConversation(conversationId), CancellationToken.None),
            Times.Once);
    }
}
