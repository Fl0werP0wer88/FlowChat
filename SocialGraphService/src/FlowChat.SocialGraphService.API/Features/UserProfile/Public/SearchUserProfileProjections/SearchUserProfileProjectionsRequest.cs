using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Public.SearchUserProfileProjections;

public sealed class SearchUserProfileProjectionsRequest : IServiceInput
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
}
