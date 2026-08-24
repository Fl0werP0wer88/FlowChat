using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;

public sealed class SearchUserProfilesRangeAscendingQueryHandler
    : IQueryHandler<SearchUserProfilesRangeAscendingQuery, SearchUserProfilesRangeAscendingPageDto>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;

    public SearchUserProfilesRangeAscendingQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    {
        _userProfileReadRepository = userProfileReadRepository;
    }

    public async Task<FlowChatResult<SearchUserProfilesRangeAscendingPageDto>> Handle(
        SearchUserProfilesRangeAscendingQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _userProfileReadRepository.SearchRangeAscendingAsync(
            Normalize(request.FirstName),
            Normalize(request.LastName),
            Normalize(request.Organization),
            Normalize(request.Cursor)?.ToLowerInvariant(),
            request.Limit + 1,
            cancellationToken);
        var hasMore = rows.Count > request.Limit;
        var items = rows.Take(request.Limit).ToList();
        var nextCursor = hasMore ? items[^1].FriendlyUserId : null;

        return FlowChatResult<SearchUserProfilesRangeAscendingPageDto>.Success(
            new SearchUserProfilesRangeAscendingPageDto(items, nextCursor, hasMore));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
