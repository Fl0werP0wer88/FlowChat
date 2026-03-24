namespace FlowChat.Domain.Abstractions;

public interface IEntity<TDomainEntity>
{
    Id<TDomainEntity> Id { get; }
    string CreatedBy { get; }
    DateTimeOffset CreatedAtUtc { get; }
    string LastModifiedBy { get; }
    DateTimeOffset LastModifiedAtUtc { get; }
}
