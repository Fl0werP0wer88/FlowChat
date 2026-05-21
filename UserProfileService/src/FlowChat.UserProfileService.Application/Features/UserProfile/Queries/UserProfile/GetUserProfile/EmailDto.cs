using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfile;

public sealed record EmailDto(
    Guid Id,
    string Address,
    bool IsMain,
    bool IsAuth,
    bool IsConfirmed) : IDbReadResponse;
