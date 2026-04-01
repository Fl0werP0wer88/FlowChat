using System.Diagnostics;
using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.Behaviors;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application.UnitTests.Behaviors;

public sealed class ExceptionHandlingPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_WhenDbUpdateConcurrencyExceptionIsThrown_ReturnsConcurrencyConflictFailure()
    {
        var behavior = new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var exception = new DbUpdateConcurrencyException("Row version mismatch.");
        using var activity = new Activity("test").Start();

        var result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.ConcurencyConflict);
        result.Error.ErrorMessage.Should().Be("Row version mismatch.");

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("concurrency_conflict");
        activity.Tags.Single(x => x.Key == "error.type").Value.Should().Be("concurrency_conflict");
        activity.Events.Should().Contain(x => x.Name == "exception");
    }

    private sealed record TestRequest : IRequest<FlowChatResult<Guid>>;
}
