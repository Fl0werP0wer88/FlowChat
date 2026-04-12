using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;

public sealed record DeleteContactCommand(Guid OwnerUserId, Guid ContactUserId) : ICommand<Unit>;
