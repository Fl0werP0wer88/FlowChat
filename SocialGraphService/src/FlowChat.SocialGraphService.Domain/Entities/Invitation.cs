using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Enums;

namespace FlowChat.SocialGraphService.Domain.Entities;

public class Invitation : EntityBase
{
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }

    public Invitation(
        Guid id,
        Guid requesterId,
        Guid addresseeId,
        InvitationStatus status = InvitationStatus.Pending,
        DateTime? respondedAtUtc = null) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
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
}
