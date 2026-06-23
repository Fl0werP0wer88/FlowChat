using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Api.Features.User.Internal.ChangeAuthEmail;

public sealed class ChangeAuthEmailRequest : IServiceInput
{
    public Guid UserId { get; init; }
    public required string EmailAddress { get; init; }
}
