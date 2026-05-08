using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;

public sealed record PhoneDto(
    Guid Id,
    string Number,
    bool IsMain) : IDbReadResponse;
