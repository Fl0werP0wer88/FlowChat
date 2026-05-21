using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.SearchUserProfiles;

public sealed class SearchUserProfilesQueryHandler
    : IQueryHandler<SearchUserProfilesQuery, IReadOnlyList<SearchUserProfileDto>>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;

    public SearchUserProfilesQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    {
        _userProfileReadRepository = userProfileReadRepository;
    }

    public async Task<FlowChatResult<IReadOnlyList<SearchUserProfileDto>>> Handle(
        SearchUserProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var userProfiles = await _userProfileReadRepository.SearchAsync(
            Normalize(request.FirstName),
            Normalize(request.LastName),
            Normalize(request.Organization),
            cancellationToken);

        return FlowChatResult<IReadOnlyList<SearchUserProfileDto>>.Success(userProfiles);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
