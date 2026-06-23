using FlowChat.Core.Contracts;

namespace FlowChat.AuthService.Api.Features.User.Internal.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailRequest : IServiceInput
{
    public required string EmailAddress { get; init; }
}
