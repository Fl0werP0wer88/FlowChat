using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.AddEmail;

public sealed class AddEmailCommandHandler
    : CommandHandlerBase<AddEmailCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public AddEmailCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<Result<Guid, IDomainError>> ExecuteAsync(
        AddEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("Email address is required."));
        }

        _userProfile = await _userProfileRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var normalizedAddress = request.Address!.Trim();
        if (_userProfile.Emails.Any(x => string.Equals(x.Address, normalizedAddress, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Conflict($"Email '{normalizedAddress}' already exists."));
        }

        var email = _userProfile.AddEmail(normalizedAddress);

        return email.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Guid, IDomainError> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
