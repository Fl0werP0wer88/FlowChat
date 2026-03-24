namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;

public sealed record PhoneDto(
    Guid Id,
    string Number,
    bool IsMain);
