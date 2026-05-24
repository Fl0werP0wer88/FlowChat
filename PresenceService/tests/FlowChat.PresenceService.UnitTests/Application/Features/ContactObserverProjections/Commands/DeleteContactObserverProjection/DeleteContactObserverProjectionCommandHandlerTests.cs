using AutoFixture;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.DeleteContactObserverProjection;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class DeleteContactObserverProjectionCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactObserverProjectionWriteRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly DeleteContactObserverProjectionCommandHandler _handler;

    public DeleteContactObserverProjectionCommandHandlerTests()
    {
        _repositoryMock
            .Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<Func<FlowChatResult<Unit>, CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<Func<Exception, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<Unit>>>,
                Func<FlowChatResult<Unit>, CancellationToken, Task<FlowChatResult<Unit>>>,
                Func<Exception, CancellationToken, Task>,
                CancellationToken>(async (operation, beforeCommitOperation, _, ct) =>
                {
                    var result = await operation(ct);
                    return await beforeCommitOperation(result, ct);
                });

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new DeleteContactObserverProjectionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenProjectionDoesNotExist_ReturnsSuccessAndCallsDelete()
    {
        var observedUserId = _fixture.Create<Guid>();
        var observerUserId = _fixture.Create<Guid>();

        var result = await _handler.Handle(
            new DeleteContactObserverProjectionCommand(observedUserId, observerUserId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.DeleteAsync(observedUserId, observerUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

