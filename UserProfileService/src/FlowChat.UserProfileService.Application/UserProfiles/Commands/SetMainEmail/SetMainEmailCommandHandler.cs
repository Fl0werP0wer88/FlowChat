using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainEmail;

public sealed class SetMainEmailCommandHandler
    : CommandHandlerBase<SetMainEmailCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public SetMainEmailCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<Result<Guid, IDomainError>> ExecuteAsync(
        SetMainEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("UserId is required."));
        }

        if (request.EmailId == Guid.Empty)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("EmailId is required."));
        }

        _userProfile = await _userProfileRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var email = _userProfile.Emails.FirstOrDefault(x => x.Id.Value == request.EmailId);
        if (email is null)
        {
            return Result.Failure<Guid, IDomainError>(
                DomainError.NotFound($"Email '{request.EmailId}' was not found for user profile '{request.UserId}'."));
        }

        _userProfile.SetMainEmail(email.Id);

        return email.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Guid, IDomainError> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
