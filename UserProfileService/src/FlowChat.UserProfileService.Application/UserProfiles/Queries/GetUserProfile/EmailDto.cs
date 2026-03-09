namespace FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;

public sealed record EmailDto(
    Guid Id,
    string Address,
    bool IsMain);
