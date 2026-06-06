using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class ChangeUserPresencePreferencesCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserPresencePreferencesWriteRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ChangeUserPresencePreferencesCommandHandler _handler;

    public ChangeUserPresencePreferencesCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPresencePreferences entity, CancellationToken _) => entity);

        _handler = new ChangeUserPresencePreferencesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Theory]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public async Task Handle_WhenNoPreferenceExists_CreatesNewPreference(PresenceStatus status)
    {
        var userId = _fixture.Create<Guid>();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPresencePreferences?)null);

        var result = await _handler.Handle(
            new ChangeUserPresencePreferencesCommand(userId, status),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.AddAsync(
                It.Is<UserPresencePreferences>(p => p.UserId == userId && p.PreferredStatus == status),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public async Task Handle_WhenPreferenceExists_UpdatesExistingPreference(PresenceStatus status)
    {
        var userId = _fixture.Create<Guid>();
        var existing = UserPresencePreferences.Create(userId, PresenceStatus.Busy);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(
            new ChangeUserPresencePreferencesCommand(userId, status),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.PreferredStatus.Should().Be(status);
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserPresencePreferences>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
