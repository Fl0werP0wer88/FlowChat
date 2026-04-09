using AutoFixture;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FluentAssertions;

namespace FlowChat.Core.UnitTests.Messaging;

public sealed class IntegrationEventEnvelopeTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public void Constructor_PopulatesOnlyExpectedBusinessHeaders()
    {
        var payload = new UserProfileCreatedIntegrationEvent
        {
            Key = _fixture.Create<Guid>().ToString("D"),
            UserProfileId = _fixture.Create<Guid>(),
            FriendlyUserId = "jdoe"
        };

        var envelope = new IntegrationEventEnvelope<UserProfileCreatedIntegrationEvent>(payload, payload.Key!);

        envelope.Headers.Should().HaveCount(5);
        envelope.Headers.OrderBy(static header => header.Key).Should().SatisfyRespectively(
            header => AssertHeader(header, IntegrationMessageHeaders.EventId),
            header => AssertHeader(header, IntegrationMessageHeaders.EventType, nameof(UserProfileCreatedIntegrationEvent)),
            header => AssertHeader(header, IntegrationMessageHeaders.EventVersion, "1"),
            header => AssertHeader(header, IntegrationMessageHeaders.OccurredOnUtc),
            header => AssertHeader(header, IntegrationMessageHeaders.Source, "user-profile-service"));
    }

    private static void AssertHeader(KeyValuePair<string, string> header, string expectedKey, string? expectedValue = null)
    {
        header.Key.Should().Be(expectedKey);

        if (expectedValue is not null)
        {
            header.Value.Should().Be(expectedValue);
        }
        else
        {
            header.Value.Should().NotBeNullOrWhiteSpace();
        }
    }
}
