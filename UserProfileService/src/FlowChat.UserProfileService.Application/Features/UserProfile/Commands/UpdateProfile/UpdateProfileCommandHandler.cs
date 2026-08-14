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

    public UpdateProfileCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessorV2<UpdateProfileCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<FlowChatResult<UserProfileAggregate?>> FetchAggregateRootAsync(
        UpdateProfileCommand request,
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
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var mutationType = FlowChat.Shared.Domain.MutationType.Unchanged;
        if (HasProfileChanged(request, AggregateRoot!))
        {
            mutationType = FlowChat.Shared.Domain.MutationType.Updated;
        }

        AggregateRoot!.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.Organization,
            request.AvatarUrl,
            request.Bio,
            request.IsActive);

        return Task.FromResult(Mutation(mutationType, AggregateRoot!.Id.Value));
    }

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
