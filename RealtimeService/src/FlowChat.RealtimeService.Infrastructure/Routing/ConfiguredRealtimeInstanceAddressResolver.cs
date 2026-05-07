using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.Infrastructure.Routing;

public sealed class ConfiguredRealtimeInstanceAddressResolver : IRealtimeInstanceAddressResolver
{
    private readonly IReadOnlyDictionary<string, Uri> _instanceAddresses;

    public ConfiguredRealtimeInstanceAddressResolver(IOptions<RealtimeInstancesSettingsSection> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _instanceAddresses = settings.Value.Instances.ToDictionary(
            static pair => pair.Key,
            pair =>
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    throw new InvalidOperationException("RealtimeApi:Instances keys must be non-empty instance ids.");
                }

                if (!Uri.TryCreate(pair.Value, UriKind.Absolute, out var address))
                {
                    throw new InvalidOperationException($"RealtimeApi:Instances:{pair.Key} must be an absolute URI.");
                }

                return address;
            },
            StringComparer.Ordinal);
    }

    public Uri Resolve(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        if (_instanceAddresses.TryGetValue(instanceId, out var address))
        {
            return address;
        }

        throw new InvalidOperationException(
            $"RealtimeApi:Instances does not contain a base address for instance '{instanceId}'.");
    }
}
