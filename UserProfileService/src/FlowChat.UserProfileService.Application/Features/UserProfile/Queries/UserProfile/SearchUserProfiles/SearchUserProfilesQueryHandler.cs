using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfiles;

public sealed class SearchUserProfilesQueryHandler
    : IQueryHandler<SearchUserProfilesQuery, IReadOnlyList<UserProfileDto>>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;

    public SearchUserProfilesQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    {
        _userProfileReadRepository = userProfileReadRepository;
    }

    public async Task<FlowChatResult<IReadOnlyList<UserProfileDto>>> Handle(
        SearchUserProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var userProfiles = await _userProfileReadRepository.SearchAsync(
            Normalize(request.FirstName),
            Normalize(request.LastName),
            Normalize(request.Organization),
            cancellationToken);

        return FlowChatResult<IReadOnlyList<UserProfileDto>>.Success(userProfiles);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
