namespace FlowChat.Core.Results;

public sealed record UpsertResult<TValue>(TValue Value, bool WasCreated);
