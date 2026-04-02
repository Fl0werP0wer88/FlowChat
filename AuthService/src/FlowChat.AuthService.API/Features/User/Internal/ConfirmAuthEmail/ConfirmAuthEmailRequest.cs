namespace FlowChat.AuthService.Api.Features.User.Internal.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailRequest
{
    public required string EmailAddress { get; init; }
}
