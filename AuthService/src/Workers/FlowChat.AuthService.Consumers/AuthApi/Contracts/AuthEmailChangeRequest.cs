using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Consumers.AuthApi.Contracts;

public sealed class AuthEmailChangeRequest : IConsumerOutput
{
    public Guid UserId { get; init; }
    public required string EmailAddress { get; init; }
}
