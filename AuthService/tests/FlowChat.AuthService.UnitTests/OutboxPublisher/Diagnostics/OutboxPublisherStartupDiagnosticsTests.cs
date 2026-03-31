using FlowChat.AuthService.OutboxPublisher.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class OutboxPublisherStartupDiagnosticsTests
{
    [Fact]
    public async Task OutboxWorkerStartupProbe_WhenAuthDbProbeFails_ThrowsInvalidOperationException()
    {
        var authDbProbeMock = new Mock<IAuthDbConnectivityProbe>();
        authDbProbeMock
            .Setup(x => x.ProbeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db is unavailable"));

        var kafkaProbeMock = new Mock<IKafkaConnectivityProbe>();
        kafkaProbeMock
            .Setup(x => x.ProbeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var startupProbe = new OutboxWorkerStartupProbe(
            authDbProbeMock.Object,
            kafkaProbeMock.Object,
            NullLogger<OutboxWorkerStartupProbe>.Instance);

        var act = () => startupProbe.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AuthDb connectivity probe failed*");
    }

    [Fact]
    public async Task OutboxWorkerStartupProbe_WhenKafkaProbeFails_ThrowsInvalidOperationException()
    {
        var authDbProbeMock = new Mock<IAuthDbConnectivityProbe>();
        authDbProbeMock
            .Setup(x => x.ProbeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var kafkaProbeMock = new Mock<IKafkaConnectivityProbe>();
        kafkaProbeMock
            .Setup(x => x.ProbeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("kafka is unavailable"));

        var startupProbe = new OutboxWorkerStartupProbe(
            authDbProbeMock.Object,
            kafkaProbeMock.Object,
            NullLogger<OutboxWorkerStartupProbe>.Instance);

        var act = () => startupProbe.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Kafka connectivity probe failed*");
    }

}
