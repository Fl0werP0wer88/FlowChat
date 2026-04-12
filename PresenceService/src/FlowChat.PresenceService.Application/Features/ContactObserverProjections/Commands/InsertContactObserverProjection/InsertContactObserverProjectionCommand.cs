using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.InsertContactObserverProjection;

public sealed record InsertContactObserverProjectionCommand(
    Guid ObservedUserId,
    Guid ObserverUserId) : ICommand<Unit>;
