using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class CommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenCommandSucceeds_ReturnsSuccessResult()
    {
        var expected = FlowChatResult<Guid>.Success(Guid.NewGuid());
        var handler = new TestCommandHandler((_, _) => Task.FromResult(expected));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected.Value);
    }

    [Fact]
    public async Task Handle_WhenCommandFails_ReturnsFailureResultWithoutThrowing()
    {
        var failure = FlowChatResult<Guid>.Failure(DomainError.Conflict("already exists"));
        var handler = new TestCommandHandler((_, _) => Task.FromResult(failure));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WhenUnexpectedExceptionIsThrown_RethrowsByDefault()
    {
        var expectedException = new InvalidOperationException("boom");
        var handler = new TestCommandHandler((_, _) => Task.FromException<FlowChatResult<Guid>>(expectedException));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();

        exception.Which.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task Handle_WhenUnexpectedExceptionIsThrown_UsesOverrideResult()
    {
        var expectedException = new InvalidOperationException("boom");
        var expectedResult = FlowChatResult<Guid>.Failure(DomainError.UnExpected("Handled"));
        var handler = new TestCommandHandler(
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

    private sealed record TestCommand : ICommand<Guid>;

    private sealed class TestCommandHandler : CommandHandlerBase<TestCommand, Guid>
    {
        private readonly Func<TestCommand, CancellationToken, Task<FlowChatResult<Guid>>> _executeAsync;
        private readonly Func<TestCommand, Exception, CancellationToken, Task<FlowChatResult<Guid>>>? _handleUnexpectedExceptionAsync;

        public TestCommandHandler(
            Func<TestCommand, CancellationToken, Task<FlowChatResult<Guid>>> executeAsync,
            Func<TestCommand, Exception, CancellationToken, Task<FlowChatResult<Guid>>>? handleUnexpectedExceptionAsync = null)
        {
            _executeAsync = executeAsync;
            _handleUnexpectedExceptionAsync = handleUnexpectedExceptionAsync;
        }

        protected override Task<FlowChatResult<Guid>> HandleCommandAsync(TestCommand request, CancellationToken cancellationToken)
            => _executeAsync(request, cancellationToken);

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
