namespace FlowChat.Core.Results;

public sealed record IdempotentCreateResult<TValue>(TValue Value, bool WasCreated);
