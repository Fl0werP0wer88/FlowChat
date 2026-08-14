using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SendEmailVerification;

public sealed class SendEmailVerificationCommandHandler
    : AggregateRootUpsertCommandHandlerBaseV3<SendEmailVerificationCommand, Guid, EmailVerificationProcess>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IEmailVerificationProcessWriteRepository _emailVerificationProcessWriteRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer;
    private UserProfileDto? _userProfile;
    private EmailDto? _email;

    public SendEmailVerificationCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IEmailVerificationProcessWriteRepository emailVerificationProcessWriteRepository,
        IEmailVerificationRequestIssuer emailVerificationRequestIssuer,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<SendEmailVerificationCommand, EmailVerificationProcess>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _emailVerificationProcessWriteRepository = emailVerificationProcessWriteRepository;
        _emailVerificationRequestIssuer = emailVerificationRequestIssuer;
    }

    protected override async Task<FlowChatResult<EmailVerificationProcess?>> FetchAggregateRootAsync(
        SendEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileReadRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<EmailVerificationProcess?>.Failure(
                DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var email = userProfile.Emails.FirstOrDefault(x => x.Id == request.EmailId);
        if (email is null)
        {
            return FlowChatResult<EmailVerificationProcess?>.Failure(
                DomainError.NotFound($"Email '{request.EmailId}' was not found for user profile '{request.UserId}'."));
        }

        if (email.IsConfirmed)
        {
            return FlowChatResult<EmailVerificationProcess?>.Failure(
                DomainError.Validation($"Email '{email.Address}' is already confirmed."));
        }

        _userProfile = userProfile;
        _email = email;

        var process = await _emailVerificationProcessWriteRepository
            .GetByEmailIdAsync(email.Id, cancellationToken);

        return FlowChatResult<EmailVerificationProcess?>.Success(process);
    }

    protected override async Task<FlowChatResult<AggregateMutation<Guid>>> ExecuteAsync(
        SendEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var mutationType = FlowChat.Shared.Domain.MutationType.Unchanged;
        if (AggregateRoot is null)
        {
            AggregateRoot = EmailVerificationProcess.Create(
                Id<DomainUserProfile>.FromGuid(_userProfile!.Id),
                Id<DomainEmail>.FromGuid(_email!.Id));

            await _emailVerificationProcessWriteRepository.AddAsync(AggregateRoot, cancellationToken);
            mutationType = FlowChat.Shared.Domain.MutationType.Created;
        }
        else
        {
            mutationType = FlowChat.Shared.Domain.MutationType.Updated;
        }

        var verificationRequest = await _emailVerificationRequestIssuer.IssueAsync(
            AggregateRoot,
            _userProfile!.Id,
            _email!.Id,
            _email.Address,
            cancellationToken);

        return Mutation(mutationType, verificationRequest.Id.Value);
    }
}
