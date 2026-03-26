using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Domain.Abstractions.ValueObjects;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddEmail;

public sealed class AddEmailCommandHandler
    : CommandHandlerBase<AddEmailCommand, Guid>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public AddEmailCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
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

        if (_userProfile.Emails.Any(x => x.Address == normalizedEmailAddress))
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Email '{normalizedEmailAddress!.Value}' already exists."));
        }

        var email = _userProfile.AddEmail(normalizedEmailAddress!.Value);

        return FlowChatResult<Guid>.Success(email.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
