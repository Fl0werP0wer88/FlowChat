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

    public SetAuthEmailCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<SetAuthEmailCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<UserProfileAggregate?>> FetchAggregateRootAsync(
        SetAuthEmailCommand request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<UserProfileAggregate?>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        return FlowChatResult<UserProfileAggregate?>.Success(userProfile);
    }

    protected override Task<FlowChatResult<Guid>> ExecuteAsync(
        SetAuthEmailCommand request,
        CancellationToken cancellationToken)
    {
        var email = AggregateRoot!.Emails.FirstOrDefault(x => x.Id.Value == request.EmailId);
        if (email is null)
        {
            return Task.FromResult(FlowChatResult<Guid>.Failure(
                DomainError.NotFound($"Email '{request.EmailId}' was not found for user profile '{request.UserId}'.")));
        }

        if (!email.IsAuth && !email.IsConfirmed)
        {
            return Task.FromResult(FlowChatResult<Guid>.Failure(
                DomainError.Validation(string.Format(EmailMustBeConfirmedMessageTemplate, email.Address.Value))));
        }

        if (!email.IsAuth)
        {
            SetUpdated();
        }

        AggregateRoot.SetAuthEmail(email.Id);

        return Task.FromResult(FlowChatResult<Guid>.Success(email.Id.Value));
    }
}
