namespace FlowChat.SocialGraphService.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
