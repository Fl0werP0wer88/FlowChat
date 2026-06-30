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
    : AggregateRootUpdateCommandHandlerBaseV2<AddPhoneCommand, Guid, UserProfileAggregate>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;

    public AddPhoneCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<AddPhoneCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
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

        var phone = _userProfile.AddPhone(Id<DomainPhone>.FromGuid(request.PhoneId), normalizedPhoneNumber!);

        return FlowChatResult<Guid>.Success(phone.Id.Value);
    }

    protected override UserProfileAggregate GetAggregateRoot() =>
        _userProfile ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
