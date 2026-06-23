using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record PhoneDto(
    Guid Id,
    string Number,
    bool IsMain,
    bool IsConfirmed,
    bool IsVisible) : IDbReadResponse;
