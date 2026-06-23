namespace FlowChat.RealtimeService.Infrastructure.Routing;

public interface IRealtimeInstanceAddressResolver
{
    Uri Resolve(string instanceId);
}
