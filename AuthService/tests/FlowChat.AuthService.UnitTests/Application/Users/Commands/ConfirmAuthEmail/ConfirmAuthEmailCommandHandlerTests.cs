using AutoFixture;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Domain.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class ConfirmAuthEmailCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IIdentityRepository> _identityRepositoryMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ConfirmAuthEmailCommandHandler _handler;

    public ConfirmAuthEmailCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ConfirmAuthEmailCommandHandler(
            _identityRepositoryMock.Object,
            _domainEventDispatcherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ConfirmsEmail_UpdatesUser_AndDispatchesAccountConfirmedDomainEvent()
    {
        var user = Identity.Restore(
            _fixture.Create<Guid>(),
            "flower",
            "flower@example.com",
            null,
            emailConfirmed: false,
            phoneNumberConfirmed: false);
        Identity? updatedUser = null;
        List<IDomainEvent> dispatchedEvents = [];

        _identityRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _identityRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Identity>(), It.IsAny<CancellationToken>()))
            .Callback<Identity, CancellationToken>((identity, _) => updatedUser = identity)
            .Returns(Task.CompletedTask);

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        updatedUser.Should().NotBeNull();
        updatedUser!.EmailConfirmed.Should().BeTrue();
        dispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AccountConfirmedDomainEvent>();
    }

    [Fact]
    public async Task Handle_WhenUserIsAlreadyConfirmed_ReturnsConflictWithoutUpdate()
    {
        var user = Identity.Restore(
            _fixture.Create<Guid>(),
            "flower",
            "flower@example.com",
            null,
            emailConfirmed: true,
            phoneNumberConfirmed: false);

        _identityRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("Email is already confirmed.");
        _identityRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Identity>(), It.IsAny<CancellationToken>()), Times.Never);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ReturnsNotFoundFailure()
    {
        _identityRepositoryMock
            .Setup(x => x.GetByEmailAsync("flower@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Identity?)null);

        var result = await _handler.Handle(
            new ConfirmAuthEmailCommand
            {
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be("User was not found.");
        _identityRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Identity>(), It.IsAny<CancellationToken>()), Times.Never);
        _domainEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
