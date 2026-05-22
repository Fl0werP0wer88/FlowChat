using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfileByEmail;

public sealed class GetUserProfileByEmailQueryHandler : IQueryHandler<GetUserProfileByEmailQuery, UserProfileDto>
{
    private readonly IUserProfileReadRepository _userProfileRepository;

    public GetUserProfileByEmailQueryHandler(IUserProfileReadRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<FlowChatResult<UserProfileDto>> Handle(
        GetUserProfileByEmailQuery request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (userProfile is null)
        {
            return FlowChatResult<UserProfileDto>.Failure(
                DomainError.NotFound($"User profile with email '{request.Email}' was not found."));
        }

        return FlowChatResult<UserProfileDto>.Success(userProfile);
    }
}
