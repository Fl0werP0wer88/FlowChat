using FluentAssertions;
using FluentValidation;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.Behaviors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.Shared.Application.UnitTests.Behaviors;

public sealed class PipelineBehaviorCompositionTests
{
    [Fact]
    public async Task Handle_ConcurrencyRetriesAreExhausted_ValidatesOnceAndReturnsSafeFailure()
    {
        var request = new TestRequest();
        var validator = new CountingValidator();
        var exceptionBehavior =
            new ExceptionHandlingPipelineBehavior<TestRequest, FlowChatResult<Guid>>();
        var validationBehavior =
            new ValidationPipelineBehaviour<TestRequest, FlowChatResult<Guid>>([validator]);
        var retryBehavior =
            new RetryPipelineBehavior<TestRequest, FlowChatResult<Guid>>(
                NullLogger<RetryPipelineBehavior<TestRequest, FlowChatResult<Guid>>>.Instance);
        var handlerExecutions = 0;

        var result = await exceptionBehavior.Handle(
            request,
            cancellationToken => validationBehavior.Handle(
                request,
                token => retryBehavior.Handle(
                    request,
                    _ =>
                    {
                        handlerExecutions++;
                        return Task.FromException<FlowChatResult<Guid>>(
                            new DbUpdateConcurrencyException("Row version mismatch."));
                    },
                    token),
                cancellationToken),
            CancellationToken.None);

        validator.Executions.Should().Be(1);
        handlerExecutions.Should().Be(3);
        result.IsFailure.Should().BeTrue();
        result.Error.ErrorMessage.Should().Be("An unexpected error occurred.");
    }

    private sealed record TestRequest : IRequest<FlowChatResult<Guid>>, IInProcessRetryableRequest;

    private sealed class CountingValidator : AbstractValidator<TestRequest>
    {
        public int Executions { get; private set; }

        public override Task<FluentValidation.Results.ValidationResult> ValidateAsync(
            ValidationContext<TestRequest> context,
            CancellationToken cancellation = default)
        {
            Executions++;
            return base.ValidateAsync(context, cancellation);
        }
    }
}
