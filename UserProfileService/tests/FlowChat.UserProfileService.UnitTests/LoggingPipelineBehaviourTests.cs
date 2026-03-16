using System.Diagnostics;
using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Application.Abstractions.Behaviors;
using FlowChat.Application.Abstractions.Observability;
using FlowChat.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class LoggingPipelineBehaviourTests
{
    [Fact]
    public async Task Handle_LogsStartAndCompletionAndMarksActivityAsOk()
    {
        var logger = new TestLogger<LoggingPipelineBehaviour<TestCommand, Result<Guid, IDomainError>>>();
        var behaviour = new LoggingPipelineBehaviour<TestCommand, Result<Guid, IDomainError>>(logger);
        var expectedId = Guid.NewGuid();
        using var collector = new ActivityCollector();

        var response = await behaviour.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success<Guid, IDomainError>(expectedId)),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(expectedId, response.Value);

        Assert.Collection(
            logger.Entries,
            entry =>
            {
                Assert.Equal(LogLevel.Information, entry.LogLevel);
                Assert.Contains("command started: TestCommand", entry.Message);
            },
            entry =>
            {
                Assert.Equal(LogLevel.Information, entry.LogLevel);
                Assert.Contains("command completed: TestCommand", entry.Message);
            });

        var activity = Assert.Single(collector.Activities);
        Assert.Equal("TestCommand", activity.DisplayName);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal("mediatr", activity.Tags.Single(x => x.Key == "messaging.system").Value);
        Assert.Equal("TestCommand", activity.Tags.Single(x => x.Key == "request.name").Value);
        Assert.Equal("command", activity.Tags.Single(x => x.Key == "request.kind").Value);
        Assert.Equal("application", activity.Tags.Single(x => x.Key == "layer").Value);
    }

    [Fact]
    public async Task Handle_LogsErrorAndMarksActivityAsError_WhenHandlerThrows()
    {
        var logger = new TestLogger<LoggingPipelineBehaviour<TestCommand, Result<Guid, IDomainError>>>();
        var behaviour = new LoggingPipelineBehaviour<TestCommand, Result<Guid, IDomainError>>(logger);
        var exception = new InvalidOperationException("boom");
        using var collector = new ActivityCollector();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => behaviour.Handle(
            new TestCommand(),
            _ => Task.FromException<Result<Guid, IDomainError>>(exception),
            CancellationToken.None));

        Assert.Same(exception, thrown);

        Assert.Collection(
            logger.Entries,
            entry =>
            {
                Assert.Equal(LogLevel.Information, entry.LogLevel);
                Assert.Contains("command started: TestCommand", entry.Message);
            },
            entry =>
            {
                Assert.Equal(LogLevel.Error, entry.LogLevel);
                Assert.Contains("command failed: TestCommand", entry.Message);
                Assert.Same(exception, entry.Exception);
            });

        var activity = Assert.Single(collector.Activities);
        Assert.Equal("TestCommand", activity.DisplayName);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("boom", activity.StatusDescription);
        var exceptionEvent = Assert.Single(activity.Events, x => x.Name == "exception");
        Assert.Contains(exceptionEvent.Tags, x => x.Key == "exception.message" && x.Value == "boom");
    }

    private sealed record TestCommand : ICommand<Guid>;

    private sealed class ActivityCollector : IDisposable
    {
        private readonly ActivityListener _listener;

        public ActivityCollector()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ApplicationActivitySource.For<TestCommand>().Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => Activities.Add(activity)
            };

            ActivitySource.AddActivityListener(_listener);
        }

        public List<Activity> Activities { get; } = [];

        public void Dispose() => _listener.Dispose();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
