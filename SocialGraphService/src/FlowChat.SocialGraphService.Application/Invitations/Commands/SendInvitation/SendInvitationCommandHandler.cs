using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Entities;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;

public sealed class SendInvitationCommandHandler : IRequestHandler<SendInvitationCommand, InvitationDto>
{
    private readonly IUserSocialGraphRepository _userSocialGraphRepository;
    private readonly IInvitationReadRepository _invitationReadRepository;
    private readonly IContactReadRepository _contactReadRepository;

    public SendInvitationCommandHandler(
        IUserSocialGraphRepository userSocialGraphRepository,
        IInvitationReadRepository invitationReadRepository,
        IContactReadRepository contactReadRepository)
    {
        _userSocialGraphRepository = userSocialGraphRepository;
        _invitationReadRepository = invitationReadRepository;
        _contactReadRepository = contactReadRepository;
    }

    public async Task<InvitationDto> Handle(
        SendInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RequesterId == Guid.Empty)
        {
            throw new ArgumentException("RequesterId is required.", nameof(request.RequesterId));
        }

        if (request.AddresseeId == Guid.Empty)
        {
            throw new ArgumentException("AddresseeId is required.", nameof(request.AddresseeId));
        }

        if (request.RequesterId == request.AddresseeId)
        {
            throw new ArgumentException("RequesterId and AddresseeId must be different.");
        }

        var contactExists = await _contactReadRepository.RelationshipExistsAsync(
            request.RequesterId,
            request.AddresseeId,
            cancellationToken);

        if (contactExists)
        {
            throw new InvalidOperationException("Contact relationship already exists.");
        }

        var pendingExists = await _invitationReadRepository.PendingBetweenUsersExistsAsync(
            request.RequesterId,
            request.AddresseeId,
            cancellationToken);

        if (pendingExists)
        {
            throw new InvalidOperationException("Pending invitation already exists.");
        }

        var invitation = new Invitation(
            Id<Invitation>.New(),
            request.RequesterId,
            request.AddresseeId);

        await _userSocialGraphRepository.AddInvitationAsync(invitation, cancellationToken);

        return new InvitationDto(
            invitation.Id.Value,
            invitation.RequesterId,
            invitation.AddresseeId,
            invitation.Status,
            invitation.RespondedAtUtc,
            invitation.CreatedAtUtc.UtcDateTime,
            invitation.LastModifiedAtUtc.UtcDateTime);
    }
}
