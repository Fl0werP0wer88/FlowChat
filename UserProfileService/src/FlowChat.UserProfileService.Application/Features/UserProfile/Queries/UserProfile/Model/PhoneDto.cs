using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record PhoneDto : IDbReadResponse
{
    public PhoneDto()
    {
    }

    public PhoneDto(Guid id, string number, bool isMain)
    {
        Id = id;
        Number = number;
        IsMain = isMain;
    }

    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public bool IsMain { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsVisible { get; init; }
}
