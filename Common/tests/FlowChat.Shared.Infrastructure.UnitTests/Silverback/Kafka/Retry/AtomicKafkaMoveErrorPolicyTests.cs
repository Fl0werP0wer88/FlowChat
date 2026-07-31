using FlowChat.Core.Exceptions;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Silverback.Messaging.Broker;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka.Retry;

public sealed class AtomicKafkaMoveErrorPolicyTests
{
    [Fact]
    public void CanHandle_OperationCanceledException_ReturnsFalse()
    {
        var implementation = CreatePolicyImplementation();

        implementation.CanHandle(null!, new OperationCanceledException()).Should().BeFalse();
    }

    [Fact]
    public void CanHandle_NonCancellationException_ReturnsTrue()
    {
        var implementation = CreatePolicyImplementation();

        implementation.CanHandle(null!, new TransientException("failure")).Should().BeTrue();
    }

    private static global::Silverback.Messaging.Consuming.ErrorHandling.IErrorPolicyImplementation CreatePolicyImplementation()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IUnitOfWork>());
        services.AddSingleton(Mock.Of<IConsumedOffsetCommitter>());
        services.AddSingleton(Mock.Of<IProducerCollection>());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILogger<AtomicKafkaMoveErrorPolicy>>(
            NullLogger<AtomicKafkaMoveErrorPolicy>.Instance);

        var serviceProvider = services.BuildServiceProvider();
        return new AtomicKafkaMoveErrorPolicy(TestTieredRetryKafkaConsumerSettingsSection.Create(), null)
            .Build(serviceProvider);
    }
}
