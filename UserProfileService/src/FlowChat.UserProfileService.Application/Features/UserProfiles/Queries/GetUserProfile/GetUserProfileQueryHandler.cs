using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;

public sealed class GetUserProfileQueryHandler : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserProfileReadRepository _userProfileRepository;

    public GetUserProfileQueryHandler(IUserProfileReadRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<FlowChatResult<UserProfileDto>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (userProfile is null)
        {
            return FlowChatResult<UserProfileDto>.Failure(
                DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        return FlowChatResult<UserProfileDto>.Success(userProfile);
    }
}
