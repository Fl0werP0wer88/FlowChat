using FlowChat.RealtimeService.Api.Realtime;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ChatHubExceptionFilterTests
{
    [Fact]
    public async Task InvokeMethodAsync_WhenNextSucceeds_ReturnsResultWithoutLogging()
    {
        var logger = new Mock<ILogger<ChatHubExceptionFilter>>();
        var filter = new ChatHubExceptionFilter(logger.Object);
        var invocationContext = CreateInvocationContext();

        var result = await filter.InvokeMethodAsync(invocationContext, _ => ValueTask.FromResult<object?>("ok"));

        result.Should().Be("ok");
        VerifyNoLoggedError(logger);
    }

    [Fact]
    public async Task InvokeMethodAsync_WhenNextThrows_LogsAndRethrows()
    {
        var logger = new Mock<ILogger<ChatHubExceptionFilter>>();
        var filter = new ChatHubExceptionFilter(logger.Object);
        var invocationContext = CreateInvocationContext();
        var exception = new InvalidOperationException("boom");

        Func<Task> act = () => filter.InvokeMethodAsync(invocationContext, _ => throw exception).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
        VerifyLoggedError(logger, exception);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenNextSucceeds_CallsNextWithoutLogging()
    {
        var logger = new Mock<ILogger<ChatHubExceptionFilter>>();
        var filter = new ChatHubExceptionFilter(logger.Object);
        var context = CreateLifetimeContext();

        await filter.OnConnectedAsync(context, _ => Task.CompletedTask);

        VerifyNoLoggedError(logger);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenNextThrows_LogsAndRethrows()
    {
        var logger = new Mock<ILogger<ChatHubExceptionFilter>>();
        var filter = new ChatHubExceptionFilter(logger.Object);
        var context = CreateLifetimeContext();
        var exception = new InvalidOperationException("boom");

        var act = () => filter.OnConnectedAsync(context, _ => throw exception);

        await act.Should().ThrowAsync<InvalidOperationException>();
        VerifyLoggedError(logger, exception);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenNextSucceeds_CallsNextWithoutLogging()
    {
        var logger = new Mock<ILogger<ChatHubExceptionFilter>>();
        var filter = new ChatHubExceptionFilter(logger.Object);
        var context = CreateLifetimeContext();

        await filter.OnDisconnectedAsync(context, null, (_, _) => Task.CompletedTask);

        VerifyNoLoggedError(logger);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenNextThrows_LogsAndRethrows()
    {
        var logger = new Mock<ILogger<ChatHubExceptionFilter>>();
        var filter = new ChatHubExceptionFilter(logger.Object);
        var context = CreateLifetimeContext();
        var exception = new InvalidOperationException("boom");

        var act = () => filter.OnDisconnectedAsync(context, null, (_, _) => throw exception);

        await act.Should().ThrowAsync<InvalidOperationException>();
        VerifyLoggedError(logger, exception);
    }

    private static HubInvocationContext CreateInvocationContext()
    {
        var hubMethod = typeof(TestHub).GetMethod(nameof(TestHub.Ping))!;
        return new HubInvocationContext(
            new TestHubCallerContext("connection-1"),
            new ServiceCollection().BuildServiceProvider(),
            new TestHub(),
            hubMethod,
            Array.Empty<object?>());
    }

    private static HubLifetimeContext CreateLifetimeContext() =>
        new(
            new TestHubCallerContext("connection-1"),
            new ServiceCollection().BuildServiceProvider(),
            new TestHub());

    private static void VerifyLoggedError(Mock<ILogger<ChatHubExceptionFilter>> logger, Exception exception) =>
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    private static void VerifyNoLoggedError(Mock<ILogger<ChatHubExceptionFilter>> logger) =>
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

    private sealed class TestHub : Hub
    {
        public void Ping()
        {
        }
    }
}
