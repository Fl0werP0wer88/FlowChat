namespace FlowChat.SocialGraphService.Domain.Common.Contracts;

public abstract class EntityBase
{
    public Guid Id { get; }
    public string CreatedBy { get; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; }
    public string LastModifiedBy { get; private set;} = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; private set;}

    protected EntityBase(Guid id)
    {
        Id = id;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }

    protected void Modified(string modifier)
    {
        LastModifiedBy = modifier;
        LastModifiedAtUtc = DateTimeOffset.UtcNow;
    }
}

