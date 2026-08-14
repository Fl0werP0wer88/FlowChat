using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.HarnessService.Application.Features.KafkaRetry.Commands.ProcessRetryPipelineTest;

public sealed record ProcessRetryPipelineTestCommand(
    Guid ScenarioId,
    RetryPipelineTestFailureKind FailureKind,
    int FailuresBeforeSuccess) : ICommand<Unit>;
