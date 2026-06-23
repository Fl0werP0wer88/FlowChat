using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class ApiSettingsManagerTests
{
    [Fact]
    public void AddSettingsSections_WhenConfigured_ReturnsConfiguredValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = "internal-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSettingsSections(configuration, typeof(InternalApiSettingsSection).Assembly);
        using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<InternalApiSettingsSection>>().Value.ApiKey.Should().Be("internal-key");
    }

    [Fact]
    public void AddSettingsSections_WhenConfigurationMissing_ReturnsDefaultValues()
    {
        var services = new ServiceCollection();
        services.AddSettingsSections(new ConfigurationBuilder().Build(), typeof(InternalApiSettingsSection).Assembly);
        using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<IOptions<InternalApiSettingsSection>>().Value.ApiKey.Should().BeEmpty();
    }
}
