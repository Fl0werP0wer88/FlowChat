using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.Shared.Infrastructure.UnitTests.Configuration;

public sealed class AddSettingsSectionsTests
{
    [Fact]
    public void AddSettingsSections_WhenConfigurationContainsSection_ReturnsBoundSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("FlowChat:ApiUrl", "https://flowchat.test"),
            ])
            .Build();

        var services = new ServiceCollection();
        services.AddSettingsSections(configuration, typeof(TestSettingsSection).Assembly);
        using var sp = services.BuildServiceProvider();

        var result = sp.GetRequiredService<IOptions<TestSettingsSection>>().Value;

        result.ApiUrl.Should().Be("https://flowchat.test");
    }

    [Fact]
    public void AddSettingsSections_WhenConfigurationDoesNotContainSection_ReturnsDefaultSettings()
    {
        var services = new ServiceCollection();
        services.AddSettingsSections(new ConfigurationBuilder().Build(), typeof(TestSettingsSection).Assembly);
        using var sp = services.BuildServiceProvider();

        var result = sp.GetRequiredService<IOptions<TestSettingsSection>>().Value;

        result.ApiUrl.Should().Be("https://localhost:5000");
    }

    public sealed class TestSettingsSection : SettingsSectionBase
    {
        public override string SectionName => "FlowChat";

        public string ApiUrl { get; set; } = "https://localhost:5000";
    }
}
