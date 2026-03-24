using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Domain.Abstractions.ValueObjects;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.AddPhone;

public sealed class AddPhoneCommandHandler
    : CommandHandlerBase<AddPhoneCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public AddPhoneCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<Result<Guid, IDomainError>> ExecuteAsync(
        AddPhoneCommand request,
        CancellationToken cancellationToken)
    {
        PhoneNumber? normalizedPhoneNumber = null;
        var validationErrors = new ValidationErrorCollector()
            .AddIf(request.UserId == Guid.Empty, "UserId is required.")
            .AddIf(string.IsNullOrWhiteSpace(request.Number), "Phone number is required.");

        if (!string.IsNullOrWhiteSpace(request.Number) &&
            !PhoneNumber.TryCreate(request.Number, out normalizedPhoneNumber))
        {
            validationErrors.AddIf(true, PhoneNumber.InvalidPhoneNumberMessage);
        }

        if (validationErrors.HasErrors)
        {
            return validationErrors.ToFailure<Guid>();
        }

        _userProfile = await _userProfileRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        if (_userProfile.Phones.Any(x => x.Number == normalizedPhoneNumber))
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Conflict($"Phone '{normalizedPhoneNumber!.Value}' already exists."));
        }

        var phone = _userProfile.AddPhone(normalizedPhoneNumber!.Value);

        return phone.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Guid, IDomainError> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
