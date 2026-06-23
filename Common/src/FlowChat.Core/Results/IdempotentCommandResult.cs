namespace FlowChat.Core.Results;

public sealed record IdempotentCommandResult<TValue>(TValue Value, bool WasAlreadyProcessed);
