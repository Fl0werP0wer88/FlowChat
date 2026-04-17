using FlowChat.RealtimeService.Infrastructure.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class KafkaSettingsManagerTests
{
    [Fact]
    public void GetRealtimeConnectionRegisteredProducerSettingsSection_WhenSectionExists_ReturnsConfiguredValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:RealtimeConnectionRegisteredProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:RealtimeConnectionRegisteredProducer:Topic"] = "realtime-registered"
            })
            .Build();

        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetRealtimeConnectionRegisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("broker:9092");
        result.Topic.Should().Be("realtime-registered");
    }

    [Fact]
    public void GetRealtimeConnectionRegisteredProducerSettingsSection_WhenSectionIsMissing_ReturnsDefaultOptions()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetRealtimeConnectionRegisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("localhost:9092");
        result.Topic.Should().Be("dev.flowchat.realtime.connection.v1");
    }

    [Fact]
    public void GetRealtimeConnectionUnregisteredProducerSettingsSection_WhenSectionExists_ReturnsConfiguredValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:RealtimeConnectionUnregisteredProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:RealtimeConnectionUnregisteredProducer:Topic"] = "realtime-unregistered"
            })
            .Build();

        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetRealtimeConnectionUnregisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("broker:9092");
        result.Topic.Should().Be("realtime-unregistered");
    }

    [Fact]
    public void GetRealtimeConnectionUnregisteredProducerSettingsSection_WhenSectionIsMissing_ReturnsDefaultOptions()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new KafkaSettingsManager(configuration);

        var result = sut.GetRealtimeConnectionUnregisteredProducerSettingsSection();

        result.BootstrapServers.Should().Be("localhost:9092");
        result.Topic.Should().Be("dev.flowchat.realtime.connection.v1");
    }
}
