using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.DeleteContactObserverProjection;

public sealed record DeleteContactObserverProjectionCommand(
    Guid ObservedUserId,
    Guid ObserverUserId) : ICommand<Unit>;
