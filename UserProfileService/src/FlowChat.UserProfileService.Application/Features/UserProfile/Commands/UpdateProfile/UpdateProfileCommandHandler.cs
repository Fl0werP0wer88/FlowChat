using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<UpdateProfileCommand, Guid, UserProfileAggregate>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;

    public UpdateProfileCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<UpdateProfileCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        CapturePreMutationSnapshot(_userProfile);

        if (HasProfileChanged(request, _userProfile))
        {
            SetUpdated();
        }

        _userProfile.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.Organization,
            request.AvatarUrl,
            request.Bio,
            request.IsActive);

        return FlowChatResult<Guid>.Success(_userProfile.Id.Value);
    }

    protected override UserProfileAggregate GetAggregateRoot() =>
        _userProfile ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    private static bool HasProfileChanged(UpdateProfileCommand request, UserProfileAggregate userProfile)
    {
        return userProfile.FirstName != NormalizeOptional(request.FirstName)
            || userProfile.LastName != NormalizeOptional(request.LastName)
            || userProfile.Organization != NormalizeOptional(request.Organization)
            || userProfile.AvatarUrl != NormalizeOptional(request.AvatarUrl)
            || userProfile.Bio != NormalizeOptional(request.Bio)
            || userProfile.IsActive != request.IsActive;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
