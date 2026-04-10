using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.UpdateProfile;

public sealed record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Organization,
    string? AvatarUrl,
    string? Bio,
    bool IsActive) : IServiceInput;
