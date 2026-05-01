using FlowChat.Core.Exceptions;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using FluentAssertions;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Subscribers;

public sealed class SubscriberBaseTests
{
    [Fact]
    public async Task HandleAsync_WhenExecuteThrowsNonTransientException_LogsInformationAndRethrows()
    {
        var loggerMock = new Mock<ILogger<TestSubscriber>>();
        var subscriber = new TestSubscriber(
            loggerMock.Object,
            (_, _) => throw new NonTransientException("boom"));

        var act = () => subscriber.HandleAsync(CreateEnvelope(new TestIntegrationEvent()), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");

        VerifyLog(loggerMock, LogLevel.Information, "Skipping TestIntegrationEvent in TestSubscriber. Reason: boom");
    }

    [Fact]
    public async Task HandleAsync_WhenExecuteThrowsUnexpectedException_LogsWarningAndRethrows()
    {
        var loggerMock = new Mock<ILogger<TestSubscriber>>();
        var subscriber = new TestSubscriber(
            loggerMock.Object,
            (_, _) => throw new InvalidOperationException("boom"));

        var act = () => subscriber.HandleAsync(CreateEnvelope(new TestIntegrationEvent()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        VerifyLog(loggerMock, LogLevel.Warning, "Transient failure while handling TestIntegrationEvent in TestSubscriber.");
    }

    [Fact]
    public async Task HandleAsync_WhenExecuteSucceeds_DoesNotLogFailure()
    {
        var loggerMock = new Mock<ILogger<TestSubscriber>>();
        var subscriber = new TestSubscriber(
            loggerMock.Object,
            (_, _) => Task.CompletedTask);

        await subscriber.HandleAsync(CreateEnvelope(new TestIntegrationEvent()), CancellationToken.None);

        VerifyNoLog(loggerMock, LogLevel.Information);
        VerifyNoLog(loggerMock, LogLevel.Warning);
    }

    [Theory]
    [InlineData("dev.flowchat.user-profile.user-profile.v1", "main")]
    [InlineData("dev.flowchat.user-profile.user-profile.v1.retry", "retry")]
    public async Task HandleAsync_WhenExecuteStarts_LogsSourceTopicAtTrace(
        string sourceTopic,
        string expectedDeliveryKind)
    {
        var loggerMock = new Mock<ILogger<TestSubscriber>>();
        var subscriber = new TestSubscriber(
            loggerMock.Object,
            (_, _) => Task.CompletedTask);

        await subscriber.HandleAsync(CreateEnvelope(new TestIntegrationEvent(), sourceTopic), CancellationToken.None);

        VerifyLog(
            loggerMock,
            LogLevel.Trace,
            $"Handling TestIntegrationEvent in TestSubscriber from {expectedDeliveryKind} topic {sourceTopic}.");
    }

    private static IInboundEnvelope<TestIntegrationEvent> CreateEnvelope(
        TestIntegrationEvent message,
        string sourceTopic = "dev.flowchat.test.v1")
    {
        var envelopeMock = new Mock<IInboundEnvelope<TestIntegrationEvent>>();
        envelopeMock.SetupGet(envelope => envelope.Message).Returns(message);
        envelopeMock
            .SetupGet(envelope => envelope.Endpoint)
            .Returns(new KafkaConsumerEndpoint(
                sourceTopic,
                Partition.Any,
                new KafkaConsumerEndpointConfiguration()));

        return envelopeMock.Object;
    }

    private static void VerifyLog(Mock<ILogger<TestSubscriber>> loggerMock, LogLevel expectedLogLevel, string expectedMessage)
    {
        loggerMock.Verify(
            logger => logger.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == expectedMessage),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static void VerifyNoLog(Mock<ILogger<TestSubscriber>> loggerMock, LogLevel logLevel)
    {
        loggerMock.Verify(
            logger => logger.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    public sealed class TestSubscriber(
        ILogger<TestSubscriber> logger,
        Func<TestIntegrationEvent, CancellationToken, Task> executeAsync)
        : SubscriberBase<TestIntegrationEvent>(logger)
    {
        protected override Task ExecuteAsync(TestIntegrationEvent message, CancellationToken cancellationToken) =>
            executeAsync(message, cancellationToken);
    }

    public sealed class TestIntegrationEvent;
}
