using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Domain.Abstractions.ValueObjects;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddPhone;

public sealed class AddPhoneCommandHandler
    : CommandHandlerBase<AddPhoneCommand, Guid>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public AddPhoneCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
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

        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        if (_userProfile.Phones.Any(x => x.Number == normalizedPhoneNumber))
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Phone '{normalizedPhoneNumber!.Value}' already exists."));
        }

        var phone = _userProfile.AddPhone(normalizedPhoneNumber!.Value);

        return phone.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
