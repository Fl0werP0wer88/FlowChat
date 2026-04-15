using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

public sealed record RegisterRealtimeConnectionCommand(
    Guid UserId,
    string? ConnectionId) : ICommand<Unit>;
