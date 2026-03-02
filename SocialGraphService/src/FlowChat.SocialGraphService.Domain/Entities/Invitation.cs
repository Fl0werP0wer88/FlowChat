using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Enums;

namespace FlowChat.SocialGraphService.Domain.Entities;

public class Invitation : EntityBase<Invitation>
{
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }

    private Invitation(
        Id<Invitation> id,
        Guid requesterId,
        Guid addresseeId,
        InvitationStatus status = InvitationStatus.Pending,
        DateTime? respondedAtUtc = null) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(requesterId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(addresseeId, Guid.Empty);

        if (requesterId == addresseeId)
        {
            throw new ArgumentException("RequesterId and AddresseeId must be different.");
        }

        if (status == InvitationStatus.Pending && respondedAtUtc.HasValue)
        {
            throw new ArgumentException("RespondedAtUtc must be null for pending invitation.", nameof(respondedAtUtc));
        }

        RequesterId = requesterId;
        AddresseeId = addresseeId;
        Status = status;
        RespondedAtUtc = respondedAtUtc;
    }

    public static Invitation Create(
        Id<Invitation> id,
        Guid requesterId,
        Guid addresseeId,
        InvitationStatus status = InvitationStatus.Pending,
        DateTime? respondedAtUtc = null)
    {
        return new Invitation(id, requesterId, addresseeId, status, respondedAtUtc);
    }

    public static Invitation Create(
        Guid id,
        Guid requesterId,
        Guid addresseeId,
        InvitationStatus status = InvitationStatus.Pending,
        DateTime? respondedAtUtc = null)
    {
        return Create(Id<Invitation>.FromGuid(id), requesterId, addresseeId, status, respondedAtUtc);
    }

    public void Accept(DateTime? respondedAtUtc = null)
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending invitations can be accepted.");
        }

        Status = InvitationStatus.Accepted;
        RespondedAtUtc = respondedAtUtc ?? DateTime.UtcNow;
    }
}
