using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SendEmailVerification;

public sealed class SendEmailVerificationCommandHandler
    : AggregateRootCommandHandlerBase<SendEmailVerificationCommand, Guid>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer;

    public SendEmailVerificationCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IEmailVerificationRequestIssuer emailVerificationRequestIssuer,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _emailVerificationRequestIssuer = emailVerificationRequestIssuer;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SendEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileReadRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var email = userProfile.Emails.FirstOrDefault(x => x.Id == request.EmailId);
        if (email is null)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.NotFound($"Email '{request.EmailId}' was not found for user profile '{request.UserId}'."));
        }

        if (email.IsConfirmed)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.Validation($"Email '{email.Address}' is already confirmed."));
        }

        var verificationRequest = await _emailVerificationRequestIssuer.IssueAsync(
            userProfile.Id,
            email.Id,
            email.Address,
            cancellationToken);

        return FlowChatResult<Guid>.Success(verificationRequest.Id.Value);
    }

}
