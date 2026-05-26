using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class TransactionalCommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenCommandSucceeds_ReturnsSuccessResult()
    {
        var expected = FlowChatResult<Guid>.Success(Guid.NewGuid());
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var handler = new TestTransactionalCommandHandler(
            unitOfWorkMock.Object,
            (_, _) => Task.FromResult(expected));

        var result = await handler.Handle(new TestTransactionalCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected.Value);
    }

    [Fact]
    public async Task Handle_WhenCommandFails_ReturnsFailureResultWithoutThrowing()
    {
        var failure = FlowChatResult<Guid>.Failure(DomainError.Conflict("already exists"));
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var handler = new TestTransactionalCommandHandler(
            unitOfWorkMock.Object,
            (_, _) => Task.FromResult(failure));

        var result = await handler.Handle(new TestTransactionalCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WhenUnexpectedExceptionIsThrown_RethrowsByDefault()
    {
        var expectedException = new InvalidOperationException("boom");
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var handler = new TestTransactionalCommandHandler(
            unitOfWorkMock.Object,
            (_, _) => Task.FromException<FlowChatResult<Guid>>(expectedException));

        var action = async () => await handler.Handle(new TestTransactionalCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionIsThrown_DelegatesToOverride()
    {
        var dbUpdateException = new DbUpdateException("db error");
        var expectedResult = FlowChatResult<Guid>.Failure(DomainError.UnExpected("Handled db update"));
        var unitOfWorkMock = CreateUnitOfWorkMock(exceptionToThrow: dbUpdateException);
        var handler = new TestTransactionalCommandHandler(
            unitOfWorkMock.Object,
            (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())),
            onDbUpdateException: (_, _, _) => Task.FromResult(expectedResult));

        var result = await handler.Handle(new TestTransactionalCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorMessage.Should().Be("Handled db update");
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionIsThrownWithNoOverride_RethrowsByDefault()
    {
        var dbUpdateException = new DbUpdateException("db error");
        var unitOfWorkMock = CreateUnitOfWorkMock(exceptionToThrow: dbUpdateException);
        var handler = new TestTransactionalCommandHandler(
            unitOfWorkMock.Object,
            (_, _) => Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid())));

        var action = async () => await handler.Handle(new TestTransactionalCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.Should().BeSameAs(dbUpdateException);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(Exception? exceptionToThrow = null)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                async (operation, cancellationToken) =>
                {
                    var result = await operation(cancellationToken);
                    if (exceptionToThrow is not null)
                    {
                        throw exceptionToThrow;
                    }
                    return result;
                });

        return unitOfWorkMock;
    }

    private sealed record TestTransactionalCommand : ICommand<Guid>;

    private sealed class TestTransactionalCommandHandler
        : TransactionalCommandHandlerBase<TestTransactionalCommand, Guid>
    {
        private readonly Func<TestTransactionalCommand, CancellationToken, Task<FlowChatResult<Guid>>> _executeAsync;
        private readonly Func<TestTransactionalCommand, DbUpdateException, CancellationToken, Task<FlowChatResult<Guid>>>? _onDbUpdateException;

        public TestTransactionalCommandHandler(
            IUnitOfWork unitOfWork,
            Func<TestTransactionalCommand, CancellationToken, Task<FlowChatResult<Guid>>> executeAsync,
            Func<TestTransactionalCommand, DbUpdateException, CancellationToken, Task<FlowChatResult<Guid>>>? onDbUpdateException = null)
            : base(unitOfWork)
        {
            _executeAsync = executeAsync;
            _onDbUpdateException = onDbUpdateException;
        }

        protected override Task<FlowChatResult<Guid>> ExecuteCommandAsync(
            TestTransactionalCommand request,
            CancellationToken cancellationToken)
            => _executeAsync(request, cancellationToken);

        protected override Task<FlowChatResult<Guid>> OnDbUpdateExceptionAfterRollbackAsync(
            TestTransactionalCommand request,
            DbUpdateException exception,
            CancellationToken cancellationToken)
            => _onDbUpdateException is null
                ? base.OnDbUpdateExceptionAfterRollbackAsync(request, exception, cancellationToken)
                : _onDbUpdateException(request, exception, cancellationToken);
    }
}
