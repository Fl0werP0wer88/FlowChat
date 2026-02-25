using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;

public sealed class SendInvitationCommandHandler : IRequestHandler<SendInvitationCommand, InvitationDto>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IContactRepository _contactRepository;

    public SendInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        IContactRepository contactRepository)
    {
        _invitationRepository = invitationRepository;
        _contactRepository = contactRepository;
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

        var contactExists = await _contactRepository.RelationshipExistsAsync(
            request.RequesterId,
            request.AddresseeId,
            cancellationToken);

        if (contactExists)
        {
            throw new InvalidOperationException("Contact relationship already exists.");
        }

        var pendingExists = await _invitationRepository.PendingBetweenUsersExistsAsync(
            request.RequesterId,
            request.AddresseeId,
            cancellationToken);

        if (pendingExists)
        {
            throw new InvalidOperationException("Pending invitation already exists.");
        }

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            RequesterId = request.RequesterId,
            AddresseeId = request.AddresseeId,
            Status = InvitationStatus.Pending,
            RespondedAtUtc = null,
            CreatedBy = "application"
        };

        await _invitationRepository.AddAsync(invitation, cancellationToken);

        return new InvitationDto(
            invitation.Id,
            invitation.RequesterId,
            invitation.AddresseeId,
            invitation.Status,
            invitation.RespondedAtUtc,
            invitation.CreatedDate,
            invitation.LastModifiedDate);
    }
}
