using FlowChat.Application.Abstractions;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.Application.Invitations.Commands.SendInvitation;

public sealed class SendInvitationCommandHandler : CommandHandlerBase<SendInvitationCommand, InvitationDto>
{
    private readonly IUserSocialGraphRepository _userSocialGraphRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IInvitationReadRepository _invitationReadRepository;
    private readonly IContactReadRepository _contactReadRepository;
    private Invitation? _aggregateRoot;

    public SendInvitationCommandHandler(
        IDomainEventDispatcher domainEventDispatcher,
        IUnitOfWork unitOfWork,
        IUserSocialGraphRepository userSocialGraphRepository,
        IInvitationRepository invitationRepository,
        IInvitationReadRepository invitationReadRepository,
        IContactReadRepository contactReadRepository)
        : base(domainEventDispatcher, unitOfWork)
    {
        _userSocialGraphRepository = userSocialGraphRepository;
        _invitationRepository = invitationRepository;
        _invitationReadRepository = invitationReadRepository;
        _contactReadRepository = contactReadRepository;
    }

    protected override async Task<Result<InvitationDto, IDomainError>> ExecuteAsync(
        SendInvitationCommand request,
        CancellationToken cancellationToken)
    {
        _aggregateRoot = null;

        if (request.RequesterId == Guid.Empty)
        {
            return Result.Failure<InvitationDto, IDomainError>(
                DomainError.BadRequest("RequesterId is required."));
        }

        if (request.AddresseeId == Guid.Empty)
        {
            return Result.Failure<InvitationDto, IDomainError>(
                DomainError.BadRequest("AddresseeId is required."));
        }

        if (request.RequesterId == request.AddresseeId)
        {
            return Result.Failure<InvitationDto, IDomainError>(
                DomainError.BadRequest("RequesterId and AddresseeId must be different."));
        }

        var contactExists = await _contactReadRepository.RelationshipExistsAsync(
            request.RequesterId,
            request.AddresseeId,
            cancellationToken);

        if (contactExists)
        {
            return Result.Failure<InvitationDto, IDomainError>(
                DomainError.Conflict("Contact relationship already exists."));
        }

        var pendingExists = await _invitationReadRepository.PendingBetweenUsersExistsAsync(
            request.RequesterId,
            request.AddresseeId,
            cancellationToken);

        if (pendingExists)
        {
            return Result.Failure<InvitationDto, IDomainError>(
                DomainError.Conflict("Pending invitation already exists."));
        }

        var socialGraph = await _userSocialGraphRepository.GetByUserIdAsync(
            request.RequesterId,
            cancellationToken);

        if (socialGraph is null)
        {
            return Result.Failure<InvitationDto, IDomainError>(
                DomainError.NotFound($"User social graph for requester '{request.RequesterId}' was not found."));
        }

        var invitation = Invitation.Create(
            request.RequesterId,
            request.AddresseeId,
            id: Id<Invitation>.New());

        _aggregateRoot = invitation;

        await _invitationRepository.AddAsync(
            invitation,
            cancellationToken);

        var response = new InvitationDto(
            invitation.Id.Value,
            invitation.RequesterId,
            invitation.AddresseeId,
            invitation.Status,
            invitation.RespondedAtUtc,
            invitation.CreatedAtUtc.UtcDateTime,
            invitation.LastModifiedAtUtc.UtcDateTime);

        return Result.Success<InvitationDto, IDomainError>(response);
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<InvitationDto, IDomainError> result)
    {
        return result.IsSuccess ? _aggregateRoot : null;
    }
}
