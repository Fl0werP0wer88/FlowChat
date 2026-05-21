using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record EmailDto(
    Guid Id,
    string Address,
    bool IsMain,
    bool IsAuth,
    bool IsConfirmed,
    bool IsVisible) : IDbReadResponse;
