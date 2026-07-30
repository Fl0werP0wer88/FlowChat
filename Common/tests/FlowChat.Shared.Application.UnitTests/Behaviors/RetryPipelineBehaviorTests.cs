using FluentAssertions;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.Behaviors;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.Shared.Application.UnitTests.Behaviors;

public sealed class RetryPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_UnmarkedRequestReturnsTransientFailure_ExecutesOnce()
    {
        var behavior = CreateBehavior<UnmarkedTestRequest>();
        var executions = 0;

        var result = await behavior.Handle(
            new UnmarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromResult(TransientFailure());
            },
            CancellationToken.None);

        executions.Should().Be(1);
        result.IsFailure.Should().BeTrue();
        result.Error.FailureKind.Should().Be(FailureKind.Transient);
    }

    [Fact]
    public async Task Handle_MarkedRequestSucceeds_ExecutesOnce()
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;

        var result = await behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromResult(Success());
            },
            CancellationToken.None);

        executions.Should().Be(1);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_MarkedRequestReturnsTransientFailureThenSucceeds_RetriesAndReturnsSuccess()
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;

        var result = await behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromResult(executions < 3 ? TransientFailure() : Success());
            },
            CancellationToken.None);

        executions.Should().Be(3);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MarkedRequestAlwaysReturnsTransientFailure_ReturnsLastFailureAfterThreeExecutions()
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;

        var result = await behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromResult(TransientFailure($"Attempt {executions} failed."));
            },
            CancellationToken.None);

        executions.Should().Be(3);
        result.IsFailure.Should().BeTrue();
        result.Error.FailureKind.Should().Be(FailureKind.Transient);
        result.Error.ErrorMessage.Should().Be("Attempt 3 failed.");
    }

    [Fact]
    public async Task Handle_MarkedRequestThrowsTransientExceptionThenSucceeds_RetriesAndReturnsSuccess()
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;

        var result = await behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return executions < 3
                    ? Task.FromException<FlowChatResult<Guid>>(new TransientException("Dependency unavailable."))
                    : Task.FromResult(Success());
            },
            CancellationToken.None);

        executions.Should().Be(3);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MarkedRequestAlwaysThrowsTransientException_RethrowsAfterThreeExecutions()
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;

        var act = () => behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromException<FlowChatResult<Guid>>(new TransientException("Dependency unavailable."));
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<TransientException>();
        executions.Should().Be(3);
    }

    [Theory]
    [InlineData(FailureKind.None)]
    [InlineData(FailureKind.Isolable)]
    public async Task Handle_MarkedRequestReturnsNonTransientFailure_DoesNotRetry(FailureKind failureKind)
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;

        var result = await behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromResult(FlowChatResult<Guid>.Failure(
                    DomainError.UnExpected("Request failed.", failureKind)));
            },
            CancellationToken.None);

        executions.Should().Be(1);
        result.IsFailure.Should().BeTrue();
        result.Error.FailureKind.Should().Be(failureKind);
    }

    [Fact]
    public async Task Handle_MarkedRequestThrowsNonTransientException_DoesNotRetry()
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;

        var act = () => behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromException<FlowChatResult<Guid>>(new NonTransientException("Request failed."));
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
        executions.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CancellationRequestedDuringRetryDelay_StopsFurtherExecutions()
    {
        var behavior = CreateBehavior<MarkedTestRequest>();
        var executions = 0;
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var act = () => behavior.Handle(
            new MarkedTestRequest(),
            _ =>
            {
                executions++;
                return Task.FromResult(TransientFailure());
            },
            cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        executions.Should().Be(1);
    }

    private static RetryPipelineBehavior<TRequest, FlowChatResult<Guid>> CreateBehavior<TRequest>()
        where TRequest : notnull, IRequest<FlowChatResult<Guid>> =>
        new(NullLogger<RetryPipelineBehavior<TRequest, FlowChatResult<Guid>>>.Instance);

    private static FlowChatResult<Guid> Success() =>
        FlowChatResult<Guid>.Success(Guid.NewGuid());

    private static FlowChatResult<Guid> TransientFailure(string message = "Dependency unavailable.") =>
        FlowChatResult<Guid>.Failure(DomainError.UnExpected(message, FailureKind.Transient));

    private sealed record UnmarkedTestRequest : IRequest<FlowChatResult<Guid>>;

    private sealed record MarkedTestRequest : IRequest<FlowChatResult<Guid>>, IInProcessRetryableRequest;
}
