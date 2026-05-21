using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfile;

public sealed record PhoneDto(
    Guid Id,
    string Number,
    bool IsMain) : IDbReadResponse;
