using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.Shared.Domain.ValueObjects;

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
        if (!PhoneNumber.TryCreate(request.Number!, out var normalizedPhoneNumber))
        {
            throw new InvalidOperationException("Validated phone number could not be normalized.");
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

        return FlowChatResult<Guid>.Success(phone.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}

