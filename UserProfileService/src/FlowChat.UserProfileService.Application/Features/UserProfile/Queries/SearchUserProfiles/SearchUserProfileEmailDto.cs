using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.SearchUserProfiles;

public sealed class SearchUserProfileEmailDto : IDbReadResponse
{
    public required string Address { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
