namespace FlowChat.AuthService.Consumers.AuthApi.Contracts;

public sealed class AuthEmailConfirmationRequest
{
    public required string EmailAddress { get; init; }
}
