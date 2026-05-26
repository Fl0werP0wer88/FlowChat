using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class CommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenUnexpectedExceptionIsThrown_RethrowsByDefault()
    {
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        var expectedException = new InvalidOperationException("boom");
        var handler = new TestCommandHandler(
            domainEventDispatcherMock.Object,
            unitOfWorkMock.Object,
            (_, _) => Task.FromException<FlowChatResult<Guid>>(expectedException));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task Handle_WhenUnexpectedExceptionIsThrown_UsesOverrideResult()
    {
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        var expectedException = new InvalidOperationException("boom");
        var expectedResult = FlowChatResult<Guid>.Failure(DomainError.UnExpected("Handled"));
        var handler = new TestCommandHandler(
            domainEventDispatcherMock.Object,
            unitOfWorkMock.Object,
            (_, _) => Task.FromException<FlowChatResult<Guid>>(expectedException),
            handleUnexpectedExceptionAsync: (_, exception, _) =>
            {
                exception.Should().BeSameAs(expectedException);
                return Task.FromResult(expectedResult);
            });

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        result.Error.ErrorMessage.Should().Be("Handled");
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionIsThrown_UsesDbUpdateExceptionOverrideResult()
    {
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        var expectedException = new DbUpdateException("boom");
        var expectedResult = FlowChatResult<Guid>.Failure(DomainError.UnExpected("Handled db update"));
        var handler = new TestCommandHandler(
            domainEventDispatcherMock.Object,
            unitOfWorkMock.Object,
            (_, _) => Task.FromException<FlowChatResult<Guid>>(expectedException),
            handleDbUpdateExceptionAsync: (_, exception, _) =>
            {
                exception.Should().BeSameAs(expectedException);
                return Task.FromResult(expectedResult);
            });

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        result.Error.ErrorMessage.Should().Be("Handled db update");
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        return unitOfWorkMock;
    }

    private sealed record TestCommand : ICommand<Guid>;

    private sealed class TestCommandHandler : CommandHandlerBase<TestCommand, Guid>
    {
        private readonly Func<TestCommand, CancellationToken, Task<FlowChatResult<Guid>>> _executeAsync;
        private readonly Func<TestCommand, DbUpdateException, CancellationToken, Task<FlowChatResult<Guid>>>? _handleDbUpdateExceptionAsync;
        private readonly Func<TestCommand, Exception, CancellationToken, Task<FlowChatResult<Guid>>>? _handleUnexpectedExceptionAsync;

        public TestCommandHandler(
            IDomainEventDispatcher domainEventDispatcher,
            IUnitOfWork unitOfWork,
            Func<TestCommand, CancellationToken, Task<FlowChatResult<Guid>>> executeAsync,
            Func<TestCommand, DbUpdateException, CancellationToken, Task<FlowChatResult<Guid>>>? handleDbUpdateExceptionAsync = null,
            Func<TestCommand, Exception, CancellationToken, Task<FlowChatResult<Guid>>>? handleUnexpectedExceptionAsync = null)
            : base(domainEventDispatcher, unitOfWork)
        {
            _executeAsync = executeAsync;
            _handleDbUpdateExceptionAsync = handleDbUpdateExceptionAsync;
            _handleUnexpectedExceptionAsync = handleUnexpectedExceptionAsync;
        }

        protected override Task<FlowChatResult<Guid>> ExecuteAsync(TestCommand request, CancellationToken cancellationToken)
        {
            return _executeAsync(request, cancellationToken);
        }

        protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
        {
            return null;
        }

        protected override Task<FlowChatResult<Guid>> OnDbUpdateExceptionAfterRollbackHook(
            TestCommand request,
            DbUpdateException exception,
            CancellationToken cancellationToken)
        {
            return _handleDbUpdateExceptionAsync is null
                ? base.OnDbUpdateExceptionAfterRollbackHook(request, exception, cancellationToken)
                : _handleDbUpdateExceptionAsync(request, exception, cancellationToken);
        }

        protected override Task<FlowChatResult<Guid>> HandleUnexpectedExceptionAsync(
            TestCommand request,
            Exception exception,
            CancellationToken cancellationToken)
        {
            return _handleUnexpectedExceptionAsync is null
                ? base.HandleUnexpectedExceptionAsync(request, exception, cancellationToken)
                : _handleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }
    }
}
