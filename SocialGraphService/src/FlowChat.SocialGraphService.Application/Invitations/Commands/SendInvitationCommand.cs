using MediatR;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands;

public sealed record SendInvitationCommand(
    Guid RequesterId,
    Guid AddresseeId) : IRequest<InvitationDto>;
