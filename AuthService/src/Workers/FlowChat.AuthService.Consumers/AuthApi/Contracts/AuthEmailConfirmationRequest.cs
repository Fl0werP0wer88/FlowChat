using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Consumers.AuthApi.Contracts;

public sealed class AuthEmailConfirmationRequest : IConsumerOutput
{
    public required string EmailAddress { get; init; }
}
