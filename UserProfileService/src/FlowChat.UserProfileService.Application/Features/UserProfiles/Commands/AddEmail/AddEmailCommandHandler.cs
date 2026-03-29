using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddEmail;

public sealed class AddEmailCommandHandler
    : CommandHandlerBase<AddEmailCommand, Guid>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer;
    private UserProfile? _userProfile;

    public AddEmailCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IUserProfileWriteRepository userProfileRepository,
        IEmailVerificationRequestIssuer emailVerificationRequestIssuer,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _userProfileRepository = userProfileRepository;
        _emailVerificationRequestIssuer = emailVerificationRequestIssuer;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        AddEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (!EmailAddress.TryCreate(request.Address!, out var normalizedEmailAddress))
        {
            throw new InvalidOperationException("Validated email address could not be normalized.");
        }

        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        if (await _userProfileReadRepository.EmailAddressExistsAsync(normalizedEmailAddress!.Value, cancellationToken))
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Email '{normalizedEmailAddress.Value}' is already taken."));
        }

        var email = _userProfile.AddEmail(normalizedEmailAddress.Value);
        await _emailVerificationRequestIssuer.IssueAsync(_userProfile, email, cancellationToken);

        return FlowChatResult<Guid>.Success(email.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
