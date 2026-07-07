using AutoFixture;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RegisterRealtimeConnectionCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly CapturingRealtimeGroupManager _groupManager = new();
    private readonly Mock<IRealtimeGroupMembershipRepository> _groupMembershipRepositoryMock = new();
    private readonly CapturingRealtimeConnectionRegistry _registry = new();
    private readonly CapturingPresenceInternalApiClient _presenceClient = new();
    private readonly RegisterRealtimeConnectionCommandHandler _handler;

    public RegisterRealtimeConnectionCommandHandlerTests()
    {
        _groupMembershipRepositoryMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RealtimeGroupMembershipDto>)[]);

        _handler = new RegisterRealtimeConnectionCommandHandler(
            _groupManager,
            _groupMembershipRepositoryMock.Object,
            _registry,
            _presenceClient,
            NullLogger<RegisterRealtimeConnectionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenConnectionRegistered_ReturnsSuccess()
    {
        var userId = _fixture.Create<Guid>();
        var conversationId = _fixture.Create<Guid>();
        _groupMembershipRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RealtimeGroupMembershipDto>)
            [
                new RealtimeGroupMembershipDto(userId, RealtimeGroupType.Conversation, conversationId, DateTimeOffset.UtcNow)
            ]);

        var result = await _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _groupManager.AddedToUserGroup.Should().ContainSingle().Which.Should().Be(("connection-1", userId));
        _groupManager.AddedToConversationGroup.Should().ContainSingle().Which.Should().Be(("connection-1", conversationId));
        _registry.LastRegisteredUserId.Should().Be(userId);
        _registry.LastRegisteredConnectionId.Should().Be("connection-1");
        _presenceClient.LastInitializePresenceStatusUserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenJoiningConversationGroupsFails_StillReturnsSuccess()
    {
        var userId = _fixture.Create<Guid>();
        _groupMembershipRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("repository unavailable"));

        var result = await _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-2"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _registry.LastRegisteredUserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WhenRegistryFails_CompensatesByUnregisteringAndRemovingUserGroupAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();
        _registry.RegisterException = new InvalidOperationException("redis unavailable");

        var result = await _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-3"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        _registry.LastUnregisteredConnectionId.Should().Be("connection-3");
        _groupManager.RemovedFromUserGroup.Should().ContainSingle().Which.Should().Be(("connection-3", userId));
    }

    [Fact]
    public async Task Handle_WhenCanceled_CompensatesByUnregisteringAndRethrows()
    {
        var userId = _fixture.Create<Guid>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        _registry.RegisterException = new OperationCanceledException(cancellationTokenSource.Token);

        var act = () => _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-canceled"),
            cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _registry.LastUnregisteredConnectionId.Should().Be("connection-canceled");
    }

    [Fact]
    public async Task Handle_WhenPresenceInitializationFails_CompensatesByUnregisteringAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();
        _presenceClient.InitializePresenceStatusException = new InvalidOperationException("presence service unavailable");

        var result = await _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-4"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        _registry.LastUnregisteredConnectionId.Should().Be("connection-4");
        _groupManager.RemovedFromUserGroup.Should().ContainSingle().Which.Should().Be(("connection-4", userId));
    }

    [Fact]
    public async Task Handle_WhenPresenceInitializationCanceled_CompensatesByUnregisteringAndRethrows()
    {
        var userId = _fixture.Create<Guid>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        _presenceClient.InitializePresenceStatusException = new OperationCanceledException(cancellationTokenSource.Token);

        var act = () => _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-presence-canceled"),
            cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _registry.LastUnregisteredConnectionId.Should().Be("connection-presence-canceled");
    }

    [Fact]
    public async Task Handle_WhenAddingToUserGroupFails_CompensatesByUnregisteringOnlyAndReturnsFailure()
    {
        var userId = _fixture.Create<Guid>();
        _groupManager.AddToUserGroupException = new InvalidOperationException("signalr unavailable");

        var result = await _handler.Handle(
            new RegisterRealtimeConnectionCommand(userId, "connection-5"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        _registry.LastUnregisteredConnectionId.Should().Be("connection-5");
        _groupManager.RemovedFromUserGroup.Should().BeEmpty();
    }
}
