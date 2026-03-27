using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SilverbackEventPublisherTests
{
    [Fact]
    public async Task PublishToOutboxAsync_WhenEventKeyIsMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var publisher = new SilverbackEventPublisher(
            services,
            NullLogger<SilverbackEventPublisher>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishToOutboxAsync(
                new UserProfileCreatedIntegrationEvent
                {
                    UserProfileId = Guid.NewGuid(),
                    UserName = "jdoe",
                    DisplayName = "John Doe"
                },
                CancellationToken.None));

        Assert.Contains("does not contain a Kafka key", exception.Message);
    }
}
