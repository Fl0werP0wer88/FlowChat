using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;

namespace FlowChat.API.Abstractions.UnitTests;

public sealed class IntegrationEventEnvelopeTests
{
    [Fact]
    public void Constructor_PopulatesOnlyExpectedBusinessHeaders()
    {
        var payload = new UserProfileCreatedIntegrationEvent
        {
            Key = Guid.NewGuid().ToString("D"),
            UserProfileId = Guid.NewGuid(),
            UserName = "jdoe",
            DisplayName = "John Doe"
        };

        var envelope = new IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>(payload, payload.Key!);

        Assert.Equal(5, envelope.Headers.Count);
        Assert.Collection(
            envelope.Headers.OrderBy(static header => header.Key),
            header => AssertHeader(header, IntegrationMessageHeaders.EventId),
            header => AssertHeader(header, IntegrationMessageHeaders.EventType, nameof(UserProfileCreatedIntegrationEvent)),
            header => AssertHeader(header, IntegrationMessageHeaders.EventVersion, "1"),
            header => AssertHeader(header, IntegrationMessageHeaders.OccurredOnUtc),
            header => AssertHeader(header, IntegrationMessageHeaders.Source, "user-profile-service"));
    }

    private static void AssertHeader(KeyValuePair<string, string> header, string expectedKey, string? expectedValue = null)
    {
        Assert.Equal(expectedKey, header.Key);

        if (expectedValue is not null)
        {
            Assert.Equal(expectedValue, header.Value);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(header.Value));
        }
    }
}
