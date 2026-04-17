using FlowChat.Core.Contracts;
using FlowChat.Shared.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.Shared.Infrastructure.UnitTests.Configuration;

public sealed class SettingsProviderTests
{
    [Fact]
    public void GetSection_WhenConfigurationContainsSection_ReturnsBoundSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("FlowChat:ApiUrl", "https://flowchat.test"),
            ])
            .Build();
        var sut = new SettingsProvider(configuration);

        var result = sut.GetSection<TestSettingsSection>();

        result.ApiUrl.Should().Be("https://flowchat.test");
    }

    [Fact]
    public void GetSection_WhenConfigurationDoesNotContainSection_ReturnsDefaultSettings()
    {
        var sut = new SettingsProvider(new ConfigurationBuilder().Build());

        var result = sut.GetSection<TestSettingsSection>();

        result.ApiUrl.Should().Be("https://localhost:5000");
    }

    public sealed class TestSettingsSection : SettingsSectionBase
    {
        public override string SectionName => "FlowChat";

        public string ApiUrl { get; set; } = "https://localhost:5000";
    }
}
