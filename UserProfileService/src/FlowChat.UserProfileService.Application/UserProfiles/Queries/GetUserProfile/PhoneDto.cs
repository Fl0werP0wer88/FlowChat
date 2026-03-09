namespace FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;

public sealed record PhoneDto(
    Guid Id,
    string Number,
    bool IsMain);
