using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetMainPhone;

public sealed class SetMainPhoneCommandHandler
    : CommandHandlerBase<SetMainPhoneCommand, Guid>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public SetMainPhoneCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<Result<Guid, IDomainError>> ExecuteAsync(
        SetMainPhoneCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = new ValidationErrorCollector()
            .AddIf(request.UserId == Guid.Empty, "UserId is required.")
            .AddIf(request.PhoneId == Guid.Empty, "PhoneId is required.");

        if (validationErrors.HasErrors)
        {
            return validationErrors.ToFailure<Guid>();
        }

        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var phone = _userProfile.Phones.FirstOrDefault(x => x.Id.Value == request.PhoneId);
        if (phone is null)
        {
            return Result.Failure<Guid, IDomainError>(
                DomainError.NotFound($"Phone '{request.PhoneId}' was not found for user profile '{request.UserId}'."));
        }

        _userProfile.SetMainPhone(phone.Id);

        return phone.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Guid, IDomainError> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
