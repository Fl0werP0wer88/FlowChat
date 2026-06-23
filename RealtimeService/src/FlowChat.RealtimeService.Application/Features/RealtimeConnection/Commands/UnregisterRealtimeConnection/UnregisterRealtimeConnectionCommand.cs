using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;

public sealed record UnregisterRealtimeConnectionCommand(string? ConnectionId) : ICommand<Unit>;
