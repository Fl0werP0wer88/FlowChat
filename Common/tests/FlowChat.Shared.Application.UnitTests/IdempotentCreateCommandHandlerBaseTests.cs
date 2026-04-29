using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class IdempotentCreateCommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenCreateSucceeds_ReturnsCreatedValueWithoutLookup()
    {
        var createdId = Guid.NewGuid();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: false);
        var handler = new TestIdempotentCreateCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateSuccessfulUnitOfWorkMock().Object,
            classifierMock.Object,
            lookups: [],
            createAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(createdId)));

        var result = await handler.Handle(new TestIdempotentCreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(createdId);
        result.Value.WasCreated.Should().BeTrue();
        handler.CreateCallCount.Should().Be(1);
        handler.LookupCallCount.Should().Be(0);
        classifierMock.Verify(
            x => x.IsExpectedUniqueConstraintViolation(
                It.IsAny<DbUpdateException>(),
                It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniqueViolationOccursDuringTransaction_ReturnsExistingValue()
    {
        var existingId = Guid.NewGuid();
        var uniqueViolation = CreateDbUpdateException();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: true);
        var handler = new TestIdempotentCreateCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateFailingUnitOfWorkMock(uniqueViolation).Object,
            classifierMock.Object,
            lookups: [(true, existingId)],
            createAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())));

        var result = await handler.Handle(new TestIdempotentCreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(existingId);
        result.Value.WasCreated.Should().BeFalse();
        handler.CreateCallCount.Should().Be(1);
        handler.LookupCallCount.Should().Be(1);
        classifierMock.Verify(
            x => x.IsExpectedUniqueConstraintViolation(
                uniqueViolation,
                It.Is<IReadOnlyCollection<string>>(constraintNames => constraintNames.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUniqueViolationOccursButResourceStillDoesNotExist_RethrowsException()
    {
        var uniqueViolation = CreateDbUpdateException();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: true);
        var handler = new TestIdempotentCreateCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateFailingUnitOfWorkMock(uniqueViolation).Object,
            classifierMock.Object,
            lookups: [(false, default)],
            createAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())));

        var action = async () => await handler.Handle(new TestIdempotentCreateCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.Should().BeSameAs(uniqueViolation);
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionIsNotExpectedUniqueViolation_RethrowsExceptionWithoutLookup()
    {
        var dbUpdateException = CreateDbUpdateException();
        var classifierMock = CreateClassifierMock(isExpectedUniqueConstraintViolation: false);
        var handler = new TestIdempotentCreateCommandHandler(
            new Mock<IDomainEventDispatcher>().Object,
            CreateFailingUnitOfWorkMock(dbUpdateException).Object,
            classifierMock.Object,
            lookups: [],
            createAsync: (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())));

        var action = async () => await handler.Handle(new TestIdempotentCreateCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.Should().BeSameAs(dbUpdateException);
        handler.LookupCallCount.Should().Be(0);
    }

    private static Mock<IUnitOfWork> CreateSuccessfulUnitOfWorkMock()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCreateResult<Guid>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCreateResult<Guid>>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        return unitOfWorkMock;
    }

    private static Mock<IUnitOfWork> CreateFailingUnitOfWorkMock(DbUpdateException exception)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCreateResult<Guid>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCreateResult<Guid>>>>, CancellationToken>(
                async (operation, cancellationToken) =>
                {
                    await operation(cancellationToken);
                    throw exception;
                });

        return unitOfWorkMock;
    }

    private static Mock<IDbUpdateExceptionClassifier> CreateClassifierMock(bool isExpectedUniqueConstraintViolation)
    {
        var classifierMock = new Mock<IDbUpdateExceptionClassifier>();
        classifierMock
            .Setup(x => x.IsExpectedUniqueConstraintViolation(
                It.IsAny<DbUpdateException>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(isExpectedUniqueConstraintViolation);

        return classifierMock;
    }

    private static DbUpdateException CreateDbUpdateException()
    {
        return new DbUpdateException("Update failed.", new InvalidOperationException("boom"));
    }

    private sealed record TestIdempotentCreateCommand : ICommand<IdempotentCreateResult<Guid>>;

    private sealed class TestIdempotentCreateCommandHandler
        : IdempotentCreateCommandHandlerBase<TestIdempotentCreateCommand, Guid>
    {
        private readonly Queue<(bool Found, Guid Value)> _lookups;
        private readonly Func<TestIdempotentCreateCommand, CancellationToken, Task<FlowChatResult<Guid>>> _createAsync;

        public TestIdempotentCreateCommandHandler(
            IDomainEventDispatcher domainEventDispatcher,
            IUnitOfWork unitOfWork,
            IDbUpdateExceptionClassifier dbUpdateExceptionClassifier,
            IEnumerable<(bool Found, Guid Value)> lookups,
            Func<TestIdempotentCreateCommand, CancellationToken, Task<FlowChatResult<Guid>>> createAsync)
            : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
        {
            _lookups = new Queue<(bool Found, Guid Value)>(lookups);
            _createAsync = createAsync;
        }

        public int CreateCallCount { get; private set; }

        public int LookupCallCount { get; private set; }

        protected override Task<(bool Found, Guid Value)> TryGetExistingAsync(
            TestIdempotentCreateCommand request,
            CancellationToken cancellationToken)
        {
            LookupCallCount++;
            return Task.FromResult(_lookups.Dequeue());
        }

        protected override Task<FlowChatResult<Guid>> CreateAsync(
            TestIdempotentCreateCommand request,
            CancellationToken cancellationToken)
        {
            CreateCallCount++;
            return _createAsync(request, cancellationToken);
        }

        protected override IAggregateRoot? GetCreatedAggregateRoot(IdempotentCreateResult<Guid> result)
        {
            return null;
        }
    }
}
