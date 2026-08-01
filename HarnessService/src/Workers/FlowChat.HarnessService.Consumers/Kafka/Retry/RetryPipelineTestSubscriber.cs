using FlowChat.HarnessService.Application.Features.KafkaRetry.Commands.ProcessRetryPipelineTest;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.HarnessService.Consumers.Kafka.Retry;

public sealed class RetryPipelineTestSubscriber(
    IMediator mediator,
    ILogger<RetryPipelineTestSubscriber> logger)
    : SubscriberBase<RetryPipelineTestIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        RetryPipelineTestIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ProcessRetryPipelineTestCommand(
                message.ScenarioId,
                message.FailureKind,
                message.FailuresBeforeSuccess),
            cancellationToken);

        ThrowIfFailure(result);
    }
}
