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

    public SetMainEmailCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<SetMainEmailCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<UserProfileAggregate?>> FetchAggregateRootAsync(
        SetMainEmailCommand request,
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
        SetMainEmailCommand request,
        CancellationToken cancellationToken)
    {
        var mutationType = FlowChat.Shared.Domain.MutationType.Unchanged;
        var email = AggregateRoot!.Emails.FirstOrDefault(x => x.Id.Value == request.EmailId);
        if (email is null)
        {
            return Task.FromResult(Failure(
                DomainError.NotFound($"Email '{request.EmailId}' was not found for user profile '{request.UserId}'.")));
        }

        if (!email.IsMain && !email.IsConfirmed)
        {
            return Task.FromResult(Failure(
                DomainError.Validation(string.Format(EmailMustBeConfirmedMessageTemplate, email.Address.Value))));
        }

        if (!email.IsMain)
        {
            mutationType = FlowChat.Shared.Domain.MutationType.Updated;
        }

        AggregateRoot.SetMainEmail(email.Id);

        return Task.FromResult(Mutation(mutationType, email.Id.Value));
    }
}
