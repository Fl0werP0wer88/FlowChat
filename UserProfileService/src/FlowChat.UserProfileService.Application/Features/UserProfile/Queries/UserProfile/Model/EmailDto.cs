using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record EmailDto : IDbReadResponse
{
    public EmailDto()
    {
    }

    public EmailDto(Guid id, string address, bool isMain, bool isAuth, bool isConfirmed)
    {
        Id = id;
        Address = address;
        IsMain = isMain;
        IsAuth = isAuth;
        IsConfirmed = isConfirmed;
    }

    public Guid Id { get; init; }
    public string Address { get; init; } = string.Empty;
    public bool IsMain { get; init; }
    public bool IsAuth { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
