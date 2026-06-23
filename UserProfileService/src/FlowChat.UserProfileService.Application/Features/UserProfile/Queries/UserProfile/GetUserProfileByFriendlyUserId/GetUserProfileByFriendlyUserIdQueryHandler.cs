using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfileByFriendlyUserId;

public sealed class GetUserProfileByFriendlyUserIdQueryHandler : IQueryHandler<GetUserProfileByFriendlyUserIdQuery, UserProfileDto>
{
    private readonly IUserProfileReadRepository _userProfileRepository;

    public GetUserProfileByFriendlyUserIdQueryHandler(IUserProfileReadRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<FlowChatResult<UserProfileDto>> Handle(
        GetUserProfileByFriendlyUserIdQuery request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByFriendlyUserIdAsync(request.FriendlyUserId, cancellationToken);

        if (userProfile is null)
        {
            return FlowChatResult<UserProfileDto>.Failure(
                DomainError.NotFound($"User profile with friendly user ID '{request.FriendlyUserId}' was not found."));
        }

        return FlowChatResult<UserProfileDto>.Success(userProfile);
    }
}
