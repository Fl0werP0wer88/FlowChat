using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainPhone;

public sealed class SetMainPhoneCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV2<SetMainPhoneCommand, Guid, UserProfileAggregate>
{
    private const string PhoneMustBeConfirmedMessageTemplate = "Phone '{0}' must be confirmed before it can be set as the main phone.";

    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;
    private bool _mainPhoneChanged;

    public SetMainPhoneCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<SetMainPhoneCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
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

        if (!phone.IsMain && !phone.IsConfirmed)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.Validation(string.Format(PhoneMustBeConfirmedMessageTemplate, phone.Number.Value)));
        }

        _mainPhoneChanged = !phone.IsMain;
        _userProfile.SetMainPhone(phone.Id);

        return FlowChatResult<Guid>.Success(phone.Id.Value);
    }

    protected override UserProfileAggregate GetAggregateRoot() =>
        _userProfile ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    protected override AggregateState GetAggregateState(SetMainPhoneCommand request, UserProfileAggregate aggregateRoot) =>
        _mainPhoneChanged ? AggregateState.Updated : AggregateState.Unchanged;
}
