using FlowChat.SocialGraphService.Domain.Common.Contracts;

namespace FlowChat.SocialGraphService.Domain.Entities;

public class Contact : EntityBase
{
    public Guid UserId1 { get; }
    public Guid UserId2 { get; }
    public bool IsBlocked { get; }
    public Guid? BlockedBy { get; }

    private Contact(
        Guid id,
        Guid userId1,
        Guid userId2,
        bool isBlocked = false,
        Guid? blockedBy = null) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(userId1, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(userId2, Guid.Empty);

        if (userId1 == userId2)
        {
            throw new ArgumentException("UserId1 and UserId2 must be different.");
        }

        if (isBlocked && blockedBy is null)
        {
            throw new ArgumentException("BlockedBy is required when contact is blocked.", nameof(blockedBy));
        }

        UserId1 = userId1;
        UserId2 = userId2;
        IsBlocked = isBlocked;
        BlockedBy = blockedBy;
    }


    public static Contact Create(
        Guid id,
        Guid userId1,
        Guid userId2,
        bool isBlocked = false,
        Guid? blockedBy = null)
    {
        return new Contact(id, userId1, userId2, isBlocked, blockedBy);
    }
}
