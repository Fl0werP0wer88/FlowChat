using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;

public sealed class SearchUserProfileProjectionsQueryHandler
    : IQueryHandler<SearchUserProfileProjectionsQuery, IReadOnlyList<UserProfileProjectionDto>>
{
    private readonly IUserProfileProjectionReadRepository _userProfileProjectionReadRepository;

    public SearchUserProfileProjectionsQueryHandler(IUserProfileProjectionReadRepository userProfileProjectionReadRepository)
    {
        _userProfileProjectionReadRepository = userProfileProjectionReadRepository;
    }

    public async Task<FlowChatResult<IReadOnlyList<UserProfileProjectionDto>>> Handle(
        SearchUserProfileProjectionsQuery request,
        CancellationToken cancellationToken)
    {
        var projections = await _userProfileProjectionReadRepository.SearchAsync(
            Normalize(request.FirstName),
            Normalize(request.LastName),
            Normalize(request.Organization),
            cancellationToken);

        return FlowChatResult<IReadOnlyList<UserProfileProjectionDto>>.Success(projections);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

