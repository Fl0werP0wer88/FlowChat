using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.Shared.Domain.ValueObjects;
using DomainPhone = FlowChat.UserProfileService.Domain.Entities.UserProfile.Phone;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;

public sealed class AddPhoneCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<AddPhoneCommand, Guid, UserProfileAggregate>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private PhoneNumber? _normalizedPhoneNumber;

    public AddPhoneCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<AddPhoneCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<UserProfileAggregate?>> FetchAggregateRootAsync(
        AddPhoneCommand request,
        CancellationToken cancellationToken)
    {
        if (!PhoneNumber.TryCreate(request.Number!, out var normalizedPhoneNumber))
        {
            throw new InvalidOperationException("Validated phone number could not be normalized.");
        }

        _normalizedPhoneNumber = normalizedPhoneNumber;

        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<UserProfileAggregate?>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        return FlowChatResult<UserProfileAggregate?>.Success(userProfile);
    }

    protected override Task<FlowChatResult<AggregateMutation<Guid>>> ExecuteAsync(
        AddPhoneCommand request,
        CancellationToken cancellationToken)
    {
        if (AggregateRoot!.Phones.Any(x => x.Number == _normalizedPhoneNumber))
        {
            return Task.FromResult(Failure(DomainError.Conflict($"Phone '{_normalizedPhoneNumber!.Value}' already exists.")));
        }

        var phone = AggregateRoot.AddPhone(Id<DomainPhone>.FromGuid(request.PhoneId), _normalizedPhoneNumber!);

        return Task.FromResult(Updated(phone.Id.Value));
    }
}
