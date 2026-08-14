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
    private EmailAddress? _normalizedEmailAddress;

    public AddEmailCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<AddEmailCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<UserProfileAggregate?>> FetchAggregateRootAsync(
        AddEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (!EmailAddress.TryCreate(request.Address!, out var normalizedEmailAddress))
        {
            throw new InvalidOperationException("Validated email address could not be normalized.");
        }

        _normalizedEmailAddress = normalizedEmailAddress;

        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<UserProfileAggregate?>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        return FlowChatResult<UserProfileAggregate?>.Success(userProfile);
    }

    protected override async Task<FlowChatResult<AggregateMutation<Guid>>> ExecuteAsync(
        AddEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (await _userProfileReadRepository.EmailAddressExistsAsync(_normalizedEmailAddress!.Value, cancellationToken))
        {
            return Failure(DomainError.Conflict($"Email '{_normalizedEmailAddress.Value}' is already taken."));
        }

        var email = AggregateRoot!.AddEmail(Id<DomainEmail>.FromGuid(request.EmailId), _normalizedEmailAddress);

        return Updated(email.Id.Value);
    }
}
