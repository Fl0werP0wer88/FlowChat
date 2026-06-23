using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;

public sealed class CreateInitialUserProfileRequest : IConsumerOutput
{
    public string FriendlyUserId { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Organization { get; set; }

    public string? Email { get; set; }

    public Guid UserId { get; set; }
}
