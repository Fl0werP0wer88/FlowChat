using AutoFixture;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.InsertContactObserverProjection;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.PresenceService.UnitTests;

public sealed class InsertContactObserverProjectionCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IContactObserverProjectionWriteRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly InsertContactObserverProjectionCommandHandler _handler;

    public InsertContactObserverProjectionCommandHandlerTests()
    {
        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<ContactObserverProjectionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>>(),
                It.IsAny<Func<FlowChatResult<IdempotentCommandResult<Unit>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>,
                Func<FlowChatResult<IdempotentCommandResult<Unit>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>,
                CancellationToken>(async (operation, beforeCommitOperation, ct) =>
                {
                    var result = await operation(ct);
                    return await beforeCommitOperation(result, ct);
                });

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new InsertContactObserverProjectionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            Mock.Of<IDbUpdateExceptionClassifier>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsProjectionAndReturnsSuccess()
    {
        ContactObserverProjectionDto? capturedProjection = null;

        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<ContactObserverProjectionDto>(), It.IsAny<CancellationToken>()))
            .Callback<ContactObserverProjectionDto, CancellationToken>((projection, _) => capturedProjection = projection)
            .Returns(Task.CompletedTask);

        var observedUserId = _fixture.Create<Guid>();
        var observerUserId = _fixture.Create<Guid>();

        var result = await _handler.Handle(
            new InsertContactObserverProjectionCommand(observedUserId, observerUserId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedProjection.Should().NotBeNull();
        capturedProjection!.ObservedUserId.Should().Be(observedUserId);
        capturedProjection.ObserverUserId.Should().Be(observerUserId);
        capturedProjection.LastModifiedAtUtc.Should().BeOnOrAfter(capturedProjection.CreatedAtUtc);
    }
}

