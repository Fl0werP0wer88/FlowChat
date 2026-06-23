using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class IdempotentCommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenCommandSucceeds_ReturnsExecutedValueWithoutLookup()
    {
        var createdId = Guid.NewGuid();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: false);
        var handler = new TestIdempotentCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateSuccessfulUnitOfWorkMock().Object,
            classifierMock.Object,
            lookups: [],
            executeCommandAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(createdId)));

        var result = await handler.Handle(new TestIdempotentCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(createdId);
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        handler.ExecuteCommandCallCount.Should().Be(1);
        handler.LookupCallCount.Should().Be(0);
        classifierMock.Verify(
            x => x.IsExpectedIdempotencyConflict(
                It.IsAny<DbUpdateException>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniqueViolationOccursDuringTransaction_ReturnsExistingValue()
    {
        var existingId = Guid.NewGuid();
        var uniqueViolation = CreateDbUpdateException();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: true);
        var handler = new TestIdempotentCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateFailingUnitOfWorkMock(uniqueViolation).Object,
            classifierMock.Object,
            lookups: [(true, existingId)],
            executeCommandAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())));

        var result = await handler.Handle(new TestIdempotentCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(existingId);
        result.Value.WasAlreadyProcessed.Should().BeTrue();
        handler.ExecuteCommandCallCount.Should().Be(1);
        handler.LookupCallCount.Should().Be(1);
        classifierMock.Verify(
            x => x.IsExpectedIdempotencyConflict(
                uniqueViolation,
                typeof(TestIdempotentCommand).FullName!),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUniqueViolationOccursButResourceStillDoesNotExist_RethrowsException()
    {
        var uniqueViolation = CreateDbUpdateException();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: true);
        var handler = new TestIdempotentCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateFailingUnitOfWorkMock(uniqueViolation).Object,
            classifierMock.Object,
            lookups: [(false, default)],
            executeCommandAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())));

        var action = async () => await handler.Handle(new TestIdempotentCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.Should().BeSameAs(uniqueViolation);
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionIsNotExpectedUniqueViolation_RethrowsExceptionWithoutLookup()
    {
        var dbUpdateException = CreateDbUpdateException();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: false);
        var handler = new TestIdempotentCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateFailingUnitOfWorkMock(dbUpdateException).Object,
            classifierMock.Object,
            lookups: [],
            executeCommandAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())));

        var action = async () => await handler.Handle(new TestIdempotentCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.Should().BeSameAs(dbUpdateException);
        handler.LookupCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenUniqueViolationOccurs_PassesIdempotencyConflictKeyToClassifier()
    {
        var uniqueViolation = CreateDbUpdateException();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: true);
        var handler = new TestIdempotentCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateFailingUnitOfWorkMock(uniqueViolation).Object,
            classifierMock.Object,
            lookups: [(true, Guid.NewGuid())],
            executeCommandAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())),
            idempotencyConflictKey: "chat.send-message");

        await handler.Handle(new TestIdempotentCommand(), CancellationToken.None);

        classifierMock.Verify(
            x => x.IsExpectedIdempotencyConflict(
                uniqueViolation,
                "chat.send-message"),
            Times.Once);
    }

    private static Mock<IUnitOfWork> CreateSuccessfulUnitOfWorkMock()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<Func<FlowChatResult<IdempotentCommandResult<Guid>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<Func<Exception, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>,
                Func<FlowChatResult<IdempotentCommandResult<Guid>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>,
                Func<Exception, CancellationToken, Task>,
                CancellationToken>(async (operation, beforeCommitOperation, _, cancellationToken) =>
                {
                    var result = await operation(cancellationToken);
                    return await beforeCommitOperation(result, cancellationToken);
                });

        return unitOfWorkMock;
    }

    private static Mock<IUnitOfWork> CreateFailingUnitOfWorkMock(DbUpdateException exception)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<Func<FlowChatResult<IdempotentCommandResult<Guid>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>>(),
                It.IsAny<Func<Exception, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<
                Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>,
                Func<FlowChatResult<IdempotentCommandResult<Guid>>, CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Guid>>>>,
                Func<Exception, CancellationToken, Task>,
                CancellationToken>(
                async (operation, beforeCommitOperation, beforeRollbackHook, cancellationToken) =>
                {
                    try
                    {
                        var result = await operation(cancellationToken);
                        await beforeCommitOperation(result, cancellationToken);
                        throw exception;
                    }
                    catch (Exception rollbackException)
                    {
                        await beforeRollbackHook(rollbackException, cancellationToken);
                        throw;
                    }
                });

        return unitOfWorkMock;
    }

    private static Mock<IDbUpdateExceptionClassifier> CreateClassifierMock(bool isExpectedUniqueConstraintViolation)
    {
        var classifierMock = new Mock<IDbUpdateExceptionClassifier>();
        classifierMock
            .Setup(x => x.IsExpectedIdempotencyConflict(
                It.IsAny<DbUpdateException>(),
                It.IsAny<string>()))
            .Returns(isExpectedUniqueConstraintViolation);

        return classifierMock;
    }

    private static DbUpdateException CreateDbUpdateException()
    {
        return new DbUpdateException("Update failed.", new InvalidOperationException("boom"));
    }

    private sealed record TestIdempotentCommand : ICommand<IdempotentCommandResult<Guid>>;

    private sealed class TestIdempotentCommandHandler
        : IdempotentCommandHandlerBase<TestIdempotentCommand, Guid>
    {
        private readonly Queue<(bool Found, Guid Value)> _lookups;
        private readonly Func<TestIdempotentCommand, CancellationToken, Task<FlowChatResult<Guid>>> _executeCommandAsync;

        public TestIdempotentCommandHandler(
            IDomainEventDispatcher domainEventDispatcher,
            IUnitOfWork unitOfWork,
            IDbUpdateExceptionClassifier dbUpdateExceptionClassifier,
            IEnumerable<(bool Found, Guid Value)> lookups,
            Func<TestIdempotentCommand, CancellationToken, Task<FlowChatResult<Guid>>> executeCommandAsync,
            string? idempotencyConflictKey = null)
            : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
        {
            _lookups = new Queue<(bool Found, Guid Value)>(lookups);
            _executeCommandAsync = executeCommandAsync;
            IdempotencyConflictKey = idempotencyConflictKey;
        }

        public int ExecuteCommandCallCount { get; private set; }

        public int LookupCallCount { get; private set; }

        private string? IdempotencyConflictKey { get; }

        protected override Task<(bool Found, Guid Value)> TryGetExistingResponseAsync(
            TestIdempotentCommand request,
            CancellationToken cancellationToken)
        {
            LookupCallCount++;
            return Task.FromResult(_lookups.Dequeue());
        }

        protected override Task<FlowChatResult<Guid>> ExecuteCommandAsync(
            TestIdempotentCommand request,
            CancellationToken cancellationToken)
        {
            ExecuteCommandCallCount++;
            return _executeCommandAsync(request, cancellationToken);
        }

        protected override IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<Guid> result)
        {
            return null;
        }

        protected override string GetIdempotencyConflictKey(TestIdempotentCommand request)
        {
            return IdempotencyConflictKey ?? base.GetIdempotencyConflictKey(request);
        }
    }
}

