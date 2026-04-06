namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Public.SearchUserProfileProjections;

public sealed class SearchUserProfileProjectionsRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
}
