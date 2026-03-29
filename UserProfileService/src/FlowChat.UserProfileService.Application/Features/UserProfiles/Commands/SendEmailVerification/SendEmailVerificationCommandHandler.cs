using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SendEmailVerification;

public sealed class SendEmailVerificationCommandHandler
    : CommandHandlerBase<SendEmailVerificationCommand, Guid>
{
    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer;

    public SendEmailVerificationCommandHandler(
        IUserProfileWriteRepository userProfileWriteRepository,
        IEmailVerificationRequestIssuer emailVerificationRequestIssuer,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileWriteRepository = userProfileWriteRepository;
        _emailVerificationRequestIssuer = emailVerificationRequestIssuer;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SendEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileWriteRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var email = userProfile.Emails.FirstOrDefault(x => x.Id.Value == request.EmailId);
        if (email is null)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.NotFound($"Email '{request.EmailId}' was not found for user profile '{request.UserId}'."));
        }

        if (email.IsConfirmed)
        {
            return FlowChatResult<Guid>.Failure(
                DomainError.Validation($"Email '{email.Address.Value}' is already confirmed."));
        }

        var verificationRequest = await _emailVerificationRequestIssuer.IssueAsync(userProfile, email, cancellationToken);

        return FlowChatResult<Guid>.Success(verificationRequest.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) => null;
}
