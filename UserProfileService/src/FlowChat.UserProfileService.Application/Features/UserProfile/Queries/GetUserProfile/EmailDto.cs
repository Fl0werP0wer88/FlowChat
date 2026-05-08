using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;

public sealed record EmailDto(
    Guid Id,
    string Address,
    bool IsMain,
    bool IsAuth,
    bool IsConfirmed) : IDbReadResponse;
