using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Domain.Common;

public interface IDomainEvent
{
    UtcDateTimeOffset OccurredOnUtc { get; }
}
