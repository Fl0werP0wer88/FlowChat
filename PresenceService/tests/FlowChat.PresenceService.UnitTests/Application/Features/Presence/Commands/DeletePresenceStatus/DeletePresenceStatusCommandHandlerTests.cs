using AutoFixture;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class DeletePresenceStatusCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IPresenceStatusStore> _presenceStatusStoreMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly DeletePresenceStatusCommandHandler _handler;

    public DeletePresenceStatusCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new DeletePresenceStatusCommandHandler(
            _presenceStatusStoreMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPresenceExists_DeletesPresenceAndReturnsSuccess()
    {
        var userId = _fixture.Create<Guid>();

        var result = await _handler.Handle(
            new DeletePresenceStatusCommand(userId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _presenceStatusStoreMock.Verify(
            x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
