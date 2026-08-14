using FlowChat.Core.Messaging;
using FluentAssertions;
using Moq;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class OutboxIntegrationEventPublisherExtensionsTests
{
    [Fact]
    public async Task PublishAsync_PayloadAndKafkaKeyProvided_PublishesEnvelopeAndPassesCancellationToken()
    {
        var publisherMock = new Mock<IOutboxIntegrationEventPublisher>();
        var payload = new TestIntegrationEvent(Guid.NewGuid());
        var kafkaKey = Guid.NewGuid().ToString();
        using var cancellationTokenSource = new CancellationTokenSource();
        IntegrationEventEnvelope<TestIntegrationEvent>? capturedEnvelope = null;
        CancellationToken capturedCancellationToken = default;

        publisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<IntegrationEventEnvelope<TestIntegrationEvent>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IntegrationEventEnvelope<TestIntegrationEvent>, CancellationToken>((envelope, cancellationToken) =>
            {
                capturedEnvelope = envelope;
                capturedCancellationToken = cancellationToken;
            })
            .Returns(Task.CompletedTask);

        await publisherMock.Object.PublishAsync(
            payload,
            kafkaKey,
            cancellationTokenSource.Token);

        capturedEnvelope.Should().NotBeNull();
        capturedEnvelope!.Payload.Should().BeSameAs(payload);
        capturedEnvelope.KafkaKey.Should().Be(kafkaKey);
        capturedCancellationToken.Should().Be(cancellationTokenSource.Token);
        publisherMock.Verify(x => x.PublishAsync(
            It.IsAny<IntegrationEventEnvelope<TestIntegrationEvent>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestIntegrationEvent(Guid Id) : IntegrationEvent;
}
