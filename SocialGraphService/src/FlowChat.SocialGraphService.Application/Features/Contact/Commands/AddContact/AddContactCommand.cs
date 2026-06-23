using FlowChat.Shared.Application;
using FlowChat.Core.Results;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;

public sealed record AddContactCommand(
    Guid Id,
    Guid OwnerUserId,
    Guid? UserId,
    string? FriendlyUserId,
    string? Email) : ICommand<IdempotentCommandResult<Guid>>
{
    public const string IdempotencyConflictKey = nameof(AddContactCommand);
}
