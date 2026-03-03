namespace FlowChat.SocialGraphService.Application.SocialGraphs;

public sealed record SocialGraphDto(
    Guid Id,
    Guid UserId,
    string Login,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsPhoneVisible,
    bool IsEmailVisible);
