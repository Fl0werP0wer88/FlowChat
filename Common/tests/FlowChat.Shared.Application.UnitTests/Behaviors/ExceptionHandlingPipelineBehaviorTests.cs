using System.Data.Common;
using System.Diagnostics;
using FluentAssertions;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.Behaviors;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application.UnitTests.Behaviors;

public sealed class ExceptionHandlingPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_WhenDbUpdateConcurrencyExceptionIsThrown_ReturnsUnexpectedFailureAndMarksActivity()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var exception = new DbUpdateConcurrencyException("Row version mismatch.");
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        result.Error.ErrorMessage.Should().Be("An unexpected error occurred.");

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("db_concurrency_failed");
        activity.GetTagItem("error.type").Should().Be("db_concurrency");
        activity.GetTagItem("db.exception.transient").Should().Be(false);
        activity.GetTagItem("db.exception.type").Should().Be(nameof(DbUpdateConcurrencyException));
        activity.GetTagItem("db.concurrency.entry_count").Should().Be(0);
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionIsThrownWithTransientInnerException_ReturnsTransientUnexpectedFailure()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var exception = new DbUpdateException("Row version mismatch.", new TestDbException(isTransient: true, sqlState: "40001"));
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        result.Error.ErrorMessage.Should().Be("An unexpected error occurred.");
        result.Error.FailureKind.Should().Be(FailureKind.Transient);

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("db_update_failed");
        activity.GetTagItem("error.type").Should().Be("db_update");
        activity.GetTagItem("db.exception.transient").Should().Be(true);
        activity.GetTagItem("db.exception.type").Should().Be(nameof(DbUpdateException));
        activity.GetTagItem("db.exception.sql_state").Should().Be("40001");
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    [Fact]
    public async Task Handle_WhenDbUpdateExceptionIsThrownWithoutTransientInnerException_ReturnsNonTransientUnexpectedFailure()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var exception = new DbUpdateException("Row version mismatch.");
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        result.Error.ErrorMessage.Should().Be("An unexpected error occurred.");
        result.Error.FailureKind.Should().Be(FailureKind.None);

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("db_update_failed");
        activity.GetTagItem("error.type").Should().Be("db_update");
        activity.GetTagItem("db.exception.transient").Should().Be(false);
        activity.GetTagItem("db.exception.type").Should().Be(nameof(DbUpdateException));
        activity.GetTagItem("db.exception.sql_state").Should().BeNull();
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    [Fact]
    public async Task Handle_WhenIsolableExceptionIsThrown_ReturnsIsolableUnexpectedFailure()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var exception = new IsolableException("One or more items in the batch contain invalid data.");
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        result.Error.ErrorMessage.Should().Be(exception.Message);
        result.Error.FailureKind.Should().Be(FailureKind.Isolable);

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("isolable_failure");
        activity.GetTagItem("error.type").Should().Be("isolable");
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    [Fact]
    public async Task Handle_WhenNonTransientExceptionIsThrown_ReturnsNonTransientUnexpectedFailure()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var exception = new NonTransientException("The message cannot be processed.");
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unexpected);
        result.Error.ErrorMessage.Should().Be(exception.Message);
        result.Error.FailureKind.Should().Be(FailureKind.None);

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("non_transient_failure");
        activity.GetTagItem("error.type").Should().Be("non_transient");
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    [Fact]
    public async Task Handle_WhenResultExceptionIsThrown_ReturnsOriginalFailure()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var domainError = DomainError.NotFound("Aggregate was not found.", FailureKind.Isolable);
        var exception = new ResultException(FlowChatResult.Failure(domainError));
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be(domainError.ErrorMessage);
        result.Error.FailureKind.Should().Be(FailureKind.Isolable);

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("result_failure");
        activity.GetTagItem("error.type").Should().Be("result");
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    [Fact]
    public async Task Handle_WhenOperationCanceledExceptionIsThrown_ReturnsBadRequestFailure()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var exception = new OperationCanceledException("The request timed out.");
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        result.Error.ErrorMessage.Should().Be("The request was canceled.");

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("request_canceled");
        activity.GetTagItem("error.type").Should().Be("canceled");
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    [Fact]
    public async Task Handle_WhenNonGenericFlowChatResultFails_ReturnsFailureWithoutValue()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<NonGenericTestRequest, FlowChatResult>();
        var exception = new FlowChatException("Invalid request.");

        var result = await behavior.Handle(
            new NonGenericTestRequest(),
            _ => Task.FromException<FlowChatResult>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.BadRequest);
        result.Error.ErrorMessage.Should().Be(exception.Message);
        typeof(FlowChatResult).GetProperty("Value").Should().BeNull();
    }

    private sealed record TestRequest : IRequest<FlowChatResult<Guid>>;

    private sealed record NonGenericTestRequest : IRequest<FlowChatResult>;

    private sealed class TestDbException(bool isTransient, string? sqlState = null) : DbException("Database exception")
    {
        public override bool IsTransient => isTransient;

        public override string? SqlState => sqlState;
    }
}
