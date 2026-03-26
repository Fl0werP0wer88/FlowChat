using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetMainEmail;

public sealed class SetMainEmailCommandHandler
    : CommandHandlerBase<SetMainEmailCommand, Guid>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public SetMainEmailCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        SetMainEmailCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = new ValidationErrorCollector()
            .AddIf(request.UserId == Guid.Empty, "UserId is required.")
            .AddIf(request.EmailId == Guid.Empty, "EmailId is required.");

        if (validationErrors.HasErrors)
        {
            return validationErrors.ToFailure<Guid>();
        }

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

        _userProfile.SetMainEmail(email.Id);

        return email.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
