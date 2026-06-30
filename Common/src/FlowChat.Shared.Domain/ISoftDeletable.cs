using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Domain;

public interface ISoftDeletable
{
    UtcDateTimeOffset? DeletedAt { get; }
    bool IsDeleted { get; }

    void Delete(UtcDateTimeOffset deletedAt);
}
