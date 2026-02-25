using MediatR;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;

public sealed record SendInvitationCommand(
    Guid RequesterId,
    Guid AddresseeId) : IRequest<InvitationDto>;
