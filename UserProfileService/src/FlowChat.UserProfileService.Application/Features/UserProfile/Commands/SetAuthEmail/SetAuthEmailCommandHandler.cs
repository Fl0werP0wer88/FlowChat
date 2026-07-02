using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;

public sealed class SetAuthEmailCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<SetAuthEmailCommand, Guid, UserProfileAggregate>
{
    private const string EmailMustBeConfirmedMessageTemplate = "Email '{0}' must be confirmed before it can be set as the auth email.";

    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;

    public SetAuthEmailCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<SetAuthEmailCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SetAuthEmailCommand request,
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

        if (!email.IsAuth && !email.IsConfirmed)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.Validation(string.Format(EmailMustBeConfirmedMessageTemplate, email.Address.Value)));
        }

        if (!email.IsAuth)
        {
            SetUpdated();
        }

        _userProfile.SetAuthEmail(email.Id);

        return FlowChatResult<Guid>.Success(email.Id.Value);
    }

    protected override UserProfileAggregate GetAggregateRoot() =>
        _userProfile ?? throw new InvalidOperationException("Aggregate root instance is not available.");
}
