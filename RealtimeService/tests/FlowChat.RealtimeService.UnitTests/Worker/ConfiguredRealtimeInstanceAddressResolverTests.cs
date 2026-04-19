using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Services;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConfiguredRealtimeInstanceAddressResolverTests
{
    [Fact]
    public void Resolve_WhenInstanceConfigured_ReturnsBaseAddress()
    {
        var resolver = new ConfiguredRealtimeInstanceAddressResolver(new RealtimeApiSettingsSection
        {
            Instances = new Dictionary<string, string>
            {
                ["instance-a"] = "http://localhost:5215"
            }
        });

        var result = resolver.Resolve("instance-a");

        result.Should().Be(new Uri("http://localhost:5215"));
    }

    [Fact]
    public void Resolve_WhenInstanceMissing_ThrowsInvalidOperationException()
    {
        var resolver = new ConfiguredRealtimeInstanceAddressResolver(new RealtimeApiSettingsSection
        {
            Instances = new Dictionary<string, string>
            {
                ["instance-a"] = "http://localhost:5215"
            }
        });

        var act = () => resolver.Resolve("missing-instance");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing-instance*");
    }
}
