using FlowChat.Core.Contracts;
using FlowChat.Shared.Consumers.Projections.Bulk;

namespace FlowChat.HarnessService.Consumers.Configuration.Settings;

public sealed class ProjectionConsumerSettingsSection : SettingsSectionBase, IProjectionBulkConsumerSettingsSection
{
    public override string SectionName => "Kafka:ProjectionConsumer";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "harness-projection";
    public string Topic { get; set; } = "test.flowchat.harness.projection.events";
    public string AutoOffsetReset { get; set; } = "Earliest";
    public int BatchSize { get; set; } = 100;
    public int BatchMaxWaitTimeMilliseconds { get; set; } = 1000;
}
