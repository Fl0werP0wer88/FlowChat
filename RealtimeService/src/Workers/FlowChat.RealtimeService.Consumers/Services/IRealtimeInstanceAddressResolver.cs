namespace FlowChat.RealtimeService.Consumers.Services;

public interface IRealtimeInstanceAddressResolver
{
    Uri Resolve(string instanceId);
}
