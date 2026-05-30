using FlowChat.Shared.Application;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;

public sealed record AddContactCommand(
    Guid Id,
    Guid OwnerUserId,
    Guid? UserId,
    string? FriendlyUserId,
    string? Email) : ICommand<Guid>;
