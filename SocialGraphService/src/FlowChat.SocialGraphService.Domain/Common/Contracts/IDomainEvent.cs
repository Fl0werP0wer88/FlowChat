namespace FlowChat.SocialGraphService.Domain.Common.Contracts;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
