using FlowChat.AuthService.Infrastructure.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.AuthService.UnitTests;

public sealed class WorkerSettingsManagerTests
{
    [Fact]
    public void GetAccountRegisteredProducerSettingsSection_WhenSectionExists_ReturnsConfiguredValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:AccountRegisteredProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:AccountRegisteredProducer:Topic"] = "account-registered"
            })
            .Build();

        var sut = new WorkerSettingsManager(configuration);

        var result = sut.GetAccountRegisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("broker:9092");
        result.Topic.Should().Be("account-registered");
    }

    [Fact]
    public void GetAccountRegisteredProducerSettingsSection_WhenSectionIsMissing_ReturnsDefaultOptions()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new WorkerSettingsManager(configuration);

        var result = sut.GetAccountRegisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("localhost:9092");
        result.Topic.Should().Be("dev.flowchat.identity.user.v1");
    }
}
