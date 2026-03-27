using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
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

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SetMainPhoneCommand request,
        CancellationToken cancellationToken)
    {
        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var phone = _userProfile.Phones.FirstOrDefault(x => x.Id.Value == request.PhoneId);
        if (phone is null)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.NotFound($"Phone '{request.PhoneId}' was not found for user profile '{request.UserId}'."));
        }

        _userProfile.SetMainPhone(phone.Id);

        return FlowChatResult<Guid>.Success(phone.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}

