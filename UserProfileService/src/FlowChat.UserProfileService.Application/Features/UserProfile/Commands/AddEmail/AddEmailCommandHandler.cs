using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;

public sealed class AddEmailCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<AddEmailCommand, Guid, UserProfileAggregate>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;

    public AddEmailCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<AddEmailCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        AddEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (!EmailAddress.TryCreate(request.Address!, out var normalizedEmailAddress))
        {
            throw new InvalidOperationException("Validated email address could not be normalized.");
        }

        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        CapturePreMutationSnapshot(_userProfile);

        if (await _userProfileReadRepository.EmailAddressExistsAsync(normalizedEmailAddress!.Value, cancellationToken))
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Email '{normalizedEmailAddress.Value}' is already taken."));
        }

        var email = _userProfile.AddEmail(Id<DomainEmail>.FromGuid(request.EmailId), normalizedEmailAddress);
        SetUpdated();

        return FlowChatResult<Guid>.Success(email.Id.Value);
    }

    protected override UserProfileAggregate GetAggregateRoot() =>
        _userProfile ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
