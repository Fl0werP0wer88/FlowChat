using System.Diagnostics;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
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
    public async Task HandleAsync_WhenExecuteThrowsUnexpectedException_LogsErrorAndRethrows()
    {
        var loggerMock = new Mock<ILogger<TestSubscriber>>();
        var subscriber = new TestSubscriber(
            loggerMock.Object,
            (_, _) => throw new InvalidOperationException("boom"));

        var act = () => subscriber.HandleAsync(CreateEnvelope(new TestIntegrationEvent()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        VerifyLog(loggerMock, LogLevel.Error, "Unexpected failure while handling TestIntegrationEvent in TestSubscriber.");
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
    [InlineData("dev.flowchat.user-profile.user-profile.v1", null, "main")]
    [InlineData("test.flowchat.harness.retry.events", null, "main")]
    [InlineData("dev.flowchat.user-profile.user-profile.v1.retry.5s", "1", "retry")]
    [InlineData("dev.flowchat.user-profile.user-profile.v1.retry.20s", "2", "retry")]
    [InlineData("dev.flowchat.user-profile.user-profile.v1.retry.60s", "3", "retry")]
    [InlineData("dev.flowchat.user-profile.user-profile.v1.retry.300s", "4", "retry")]
    public async Task HandleAsync_WhenExecuteStarts_SetsActivityTagsWithEventContext(
        string sourceTopic,
        string? retryAttempt,
        string expectedDeliveryKind)
    {
        using var activity = new Activity("test").Start();
        var loggerMock = new Mock<ILogger<TestSubscriber>>();
        var subscriber = new TestSubscriber(
            loggerMock.Object,
            (_, _) => Task.CompletedTask);

        await subscriber.HandleAsync(
            CreateEnvelope(new TestIntegrationEvent(), sourceTopic, retryAttempt),
            CancellationToken.None);

        activity.Tags.Should()
            .Contain(new KeyValuePair<string, string?>("flowchat.subscriber.event_type", "TestIntegrationEvent"))
            .And.Contain(new KeyValuePair<string, string?>("flowchat.subscriber.name", "TestSubscriber"))
            .And.Contain(new KeyValuePair<string, string?>("flowchat.subscriber.delivery_kind", expectedDeliveryKind))
            .And.Contain(new KeyValuePair<string, string?>("flowchat.subscriber.source_topic", sourceTopic))
            .And.Contain(new KeyValuePair<string, string?>("flowchat.subscriber.message_id", "test-message-id"));
    }

    private static IInboundEnvelope<TestIntegrationEvent> CreateEnvelope(
        TestIntegrationEvent message,
        string sourceTopic = "dev.flowchat.test.v1",
        string? retryAttempt = null)
    {
        var envelopeMock = new Mock<IInboundEnvelope<TestIntegrationEvent>>();
        var headers = new MessageHeaderCollection(2);
        headers.Add(IntegrationMessageHeaders.EventId, "test-message-id");
        if (retryAttempt is not null)
            headers.Add(RetryMessageHeaders.RetryAttempt, retryAttempt);

        envelopeMock.SetupGet(envelope => envelope.Message).Returns(message);
        envelopeMock.SetupGet(envelope => envelope.Headers).Returns(headers);
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
