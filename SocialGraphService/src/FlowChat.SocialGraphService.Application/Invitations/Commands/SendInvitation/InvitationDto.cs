using FlowChat.SocialGraphService.Domain.Enums;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;

public sealed record InvitationDto(
    Guid Id,
    Guid RequesterId,
    Guid AddresseeId,
    InvitationStatus Status,
    DateTime? RespondedAtUtc,
    DateTime CreatedDate,
    DateTime LastModifiedDate);
