using FlowChat.AuthService.Infrastructure.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.AuthService.UnitTests;

public sealed class KafkaSettingsManagerTests
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

        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetAccountRegisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("broker:9092");
        result.Topic.Should().Be("account-registered");
    }

    [Fact]
    public void GetAccountRegisteredProducerSettingsSection_WhenSectionIsMissing_ReturnsDefaultOptions()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetAccountRegisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("localhost:9092");
        result.Topic.Should().Be("dev.flowchat.identity.user.v1");
    }

    [Fact]
    public void GetAccountConfirmedProducerSettingsSection_WhenSectionExists_ReturnsConfiguredValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:AccountConfirmedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:AccountConfirmedProducer:Topic"] = "account-confirmed"
            })
            .Build();

        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetAccountConfirmedProducerSettingsSection();

        result.BootstrapServers.Should().Be("broker:9092");
        result.Topic.Should().Be("account-confirmed");
    }

    [Fact]
    public void GetAccountConfirmedProducerSettingsSection_WhenSectionIsMissing_ReturnsDefaultOptions()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetAccountConfirmedProducerSettingsSection();

        result.BootstrapServers.Should().Be("localhost:9092");
        result.Topic.Should().Be("dev.flowchat.identity.user.v1");
    }
}
