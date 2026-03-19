namespace FlowChat.AuthService.API.Features.Users.ConfirmUserEmail;

public sealed class ConfirmUserEmailRequest
{
    public Guid UserId { get; set; }
    public required string Token { get; set; }
}
