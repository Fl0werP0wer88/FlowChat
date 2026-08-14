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
    : AggregateRootUpdateCommandHandlerBaseV3<SetMainPhoneCommand, Guid, UserProfileAggregate>
{
    private const string PhoneMustBeConfirmedMessageTemplate = "Phone '{0}' must be confirmed before it can be set as the main phone.";

    private readonly IUserProfileWriteRepository _userProfileRepository;

    public SetMainPhoneCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<SetMainPhoneCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<UserProfileAggregate?>> FetchAggregateRootAsync(
        SetMainPhoneCommand request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<UserProfileAggregate?>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        return FlowChatResult<UserProfileAggregate?>.Success(userProfile);
    }

    protected override Task<FlowChatResult<AggregateMutation<Guid>>> ExecuteAsync(
        SetMainPhoneCommand request,
        CancellationToken cancellationToken)
    {
        var mutationType = FlowChat.Shared.Domain.MutationType.Unchanged;
        var phone = AggregateRoot!.Phones.FirstOrDefault(x => x.Id.Value == request.PhoneId);
        if (phone is null)
        {
            return Task.FromResult(Failure(
                DomainError.NotFound($"Phone '{request.PhoneId}' was not found for user profile '{request.UserId}'.")));
        }

        if (!phone.IsMain && !phone.IsConfirmed)
        {
            return Task.FromResult(Failure(
                DomainError.Validation(string.Format(PhoneMustBeConfirmedMessageTemplate, phone.Number.Value))));
        }

        if (!phone.IsMain)
        {
            mutationType = FlowChat.Shared.Domain.MutationType.Updated;
        }

        AggregateRoot.SetMainPhone(phone.Id);

        return Task.FromResult(Mutation(mutationType, phone.Id.Value));
    }
}
