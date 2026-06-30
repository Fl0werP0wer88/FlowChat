namespace FlowChat.Shared.Domain;

public interface IEntity<TDomainEntity>
{
    Id<TDomainEntity> Id { get; }
}

