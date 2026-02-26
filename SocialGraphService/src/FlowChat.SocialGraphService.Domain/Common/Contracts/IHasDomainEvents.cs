namespace FlowChat.SocialGraphService.Domain.Common.Contracts;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
