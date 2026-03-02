using FlowChat.SocialGraphService.Application.Contracts;
using FlowChat.SocialGraphService.Application.Invitations;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;

public sealed record SendInvitationCommand(
    Guid RequesterId,
    Guid AddresseeId) : ICommand<InvitationDto>;
