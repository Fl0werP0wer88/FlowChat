using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfiles;

public sealed class GetUserProfilesQueryHandler
    : IQueryHandler<GetUserProfilesQuery, IReadOnlyList<UserProfileDto>>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;

    public GetUserProfilesQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    {
        _userProfileReadRepository = userProfileReadRepository;
    }

    public async Task<FlowChatResult<IReadOnlyList<UserProfileDto>>> Handle(
        GetUserProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var userIds = Normalize(request.UserIds);
        var userProfiles = await _userProfileReadRepository.GetByIdsAsync(userIds, cancellationToken);

        return FlowChatResult<IReadOnlyList<UserProfileDto>>.Success(userProfiles);
    }

    private static IReadOnlyList<Guid> Normalize(IReadOnlyList<Guid> userIds) =>
        userIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToList();
}
