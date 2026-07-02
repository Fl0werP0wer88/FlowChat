using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainEmail;

public sealed class SetMainEmailCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<SetMainEmailCommand, Guid, UserProfileAggregate>
{
    private const string EmailMustBeConfirmedMessageTemplate = "Email '{0}' must be confirmed before it can be set as the main email.";

    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;

    public SetMainEmailCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<SetMainEmailCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SetMainEmailCommand request,
        CancellationToken cancellationToken)
    {
        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var email = _userProfile.Emails.FirstOrDefault(x => x.Id.Value == request.EmailId);
        if (email is null)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.NotFound($"Email '{request.EmailId}' was not found for user profile '{request.UserId}'."));
        }

        if (!email.IsMain && !email.IsConfirmed)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.Validation(string.Format(EmailMustBeConfirmedMessageTemplate, email.Address.Value)));
        }

        if (!email.IsMain)
        {
            SetUpdated();
        }

        _userProfile.SetMainEmail(email.Id);

        return FlowChatResult<Guid>.Success(email.Id.Value);
    }

    protected override UserProfileAggregate GetAggregateRoot() =>
        _userProfile ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
