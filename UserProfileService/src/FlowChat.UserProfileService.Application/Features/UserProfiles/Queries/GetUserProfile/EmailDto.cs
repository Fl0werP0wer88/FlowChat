namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;

public sealed record EmailDto(
    Guid Id,
    string Address,
    bool IsMain,
    bool IsAuth);
