using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public static class KafkaConsumerGroupReadiness
{
    public static async Task WaitAsync(
        string bootstrapServers,
        IReadOnlyCollection<string> groupIds,
        TimeSpan timeout)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        }).Build();

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var report = await adminClient.DescribeConsumerGroupsAsync(
                    groupIds,
                    new DescribeConsumerGroupsOptions { RequestTimeout = TimeSpan.FromSeconds(2) });
                if (report.ConsumerGroupDescriptions.Count == groupIds.Count &&
                    report.ConsumerGroupDescriptions.All(group =>
                        group.Error.Code == ErrorCode.NoError &&
                        group.State == ConsumerGroupState.Stable &&
                        group.Members.Count > 0))
                {
                    return;
                }
            }
            catch (KafkaException)
            {
                // The groups are created asynchronously when consumers begin polling
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Timed out waiting for Kafka consumer groups to become stable.");
    }
}
