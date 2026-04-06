using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;

public sealed class SearchUserProfileProjectionsQueryHandler
    : IQueryHandler<SearchUserProfileProjectionsQuery, IReadOnlyList<UserProfileProjection>>
{
    private readonly IUserProfileProjectionReadRepository _userProfileProjectionReadRepository;

    public SearchUserProfileProjectionsQueryHandler(IUserProfileProjectionReadRepository userProfileProjectionReadRepository)
    {
        _userProfileProjectionReadRepository = userProfileProjectionReadRepository;
    }

    public async Task<FlowChatResult<IReadOnlyList<UserProfileProjection>>> Handle(
        SearchUserProfileProjectionsQuery request,
        CancellationToken cancellationToken)
    {
        var projections = await _userProfileProjectionReadRepository.SearchAsync(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.Organization.Trim(),
            cancellationToken);

        return FlowChatResult<IReadOnlyList<UserProfileProjection>>.Success(projections);
    }
}
