using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class ChatHubExceptionFilter(ILogger<ChatHubExceptionFilter> logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception while invoking hub method {HubMethodName} for connection {ConnectionId}.",
                invocationContext.HubMethodName,
                invocationContext.Context.ConnectionId);
            throw;
        }
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception while connecting hub {HubName} for connection {ConnectionId}.",
                context.Hub.GetType().Name,
                context.Context.ConnectionId);
            throw;
        }
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        try
        {
            await next(context, exception);
        }
        catch (Exception unhandledException)
        {
            logger.LogError(
                unhandledException,
                "Unhandled exception while disconnecting hub {HubName} for connection {ConnectionId}.",
                context.Hub.GetType().Name,
                context.Context.ConnectionId);
            throw;
        }
    }
}
