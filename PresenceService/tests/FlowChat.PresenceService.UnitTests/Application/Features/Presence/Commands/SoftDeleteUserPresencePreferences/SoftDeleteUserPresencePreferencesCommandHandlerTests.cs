using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.SoftDeleteUserPresencePreferences;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class SoftDeleteUserPresencePreferencesCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserPresencePreferencesWriteRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly SoftDeleteUserPresencePreferencesCommandHandler _handler;

    public SoftDeleteUserPresencePreferencesCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _repositoryMock
            .Setup(x => x.SoftDeleteAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SoftDeleteUserPresencePreferencesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPreferenceExists_SoftDeletesIt()
    {
        var userId = _fixture.Create<Guid>();
        var existing = UserPresencePreferences.Create(userId, PresenceStatus.Busy);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(
            new SoftDeleteUserPresencePreferencesCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(
                It.Is<UserPresencePreferences>(p => p.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoPreferenceExists_ReturnsSuccessWithoutDeleting()
    {
        var userId = _fixture.Create<Guid>();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPresencePreferences?)null);

        var result = await _handler.Handle(
            new SoftDeleteUserPresencePreferencesCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
