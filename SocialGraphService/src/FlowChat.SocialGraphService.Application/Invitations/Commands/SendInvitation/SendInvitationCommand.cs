using FlowChat.Application.Abstractions;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;

public sealed record SendInvitationCommand(
    Guid RequesterId,
    Guid AddresseeId) : ICommand<InvitationDto>;
