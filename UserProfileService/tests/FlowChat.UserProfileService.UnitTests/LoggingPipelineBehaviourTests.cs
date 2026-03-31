using System.Diagnostics;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Behaviors;
using FlowChat.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class LoggingPipelineBehaviourTests
{
    [Fact]
    public async Task Handle_LogsStartAndCompletionAndMarksActivityAsOk()
    {
        var logger = new TestLogger<LoggingPipelineBehaviour<TestCommand, FlowChatResult<Guid>>>();
        var behaviour = new LoggingPipelineBehaviour<TestCommand, FlowChatResult<Guid>>(logger);
        var expectedId = Guid.NewGuid();
        using var collector = new ActivityCollector();

        var response = await behaviour.Handle(
            new TestCommand(),
            _ => Task.FromResult(FlowChatResult<Guid>.Success(expectedId)),
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be(expectedId);

        logger.Entries.Should().HaveCount(2);
        logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain("command started: TestCommand");
        logger.Entries[1].LogLevel.Should().Be(LogLevel.Information);
        logger.Entries[1].Message.Should().Contain("command completed: TestCommand");

        var activity = collector.Activities.Should().ContainSingle().Subject;
        activity.DisplayName.Should().Be("TestCommand");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.Tags.Single(x => x.Key == "messaging.system").Value.Should().Be("mediatr");
        activity.Tags.Single(x => x.Key == "request.name").Value.Should().Be("TestCommand");
        activity.Tags.Single(x => x.Key == "request.kind").Value.Should().Be("command");
        activity.Tags.Single(x => x.Key == "layer").Value.Should().Be("application");
    }

    [Fact]
    public async Task Handle_LogsErrorAndMarksActivityAsError_WhenHandlerThrows()
    {
        var logger = new TestLogger<LoggingPipelineBehaviour<TestCommand, FlowChatResult<Guid>>>();
        var behaviour = new LoggingPipelineBehaviour<TestCommand, FlowChatResult<Guid>>(logger);
        var exception = new InvalidOperationException("boom");
        using var collector = new ActivityCollector();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => behaviour.Handle(
            new TestCommand(),
            _ => Task.FromException<FlowChatResult<Guid>>(exception),
            CancellationToken.None));

        thrown.Should().BeSameAs(exception);

        logger.Entries.Should().HaveCount(2);
        logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain("command started: TestCommand");
        logger.Entries[1].LogLevel.Should().Be(LogLevel.Error);
        logger.Entries[1].Message.Should().Contain("command failed: TestCommand");
        logger.Entries[1].Exception.Should().BeSameAs(exception);

        var activity = collector.Activities.Should().ContainSingle().Subject;
        activity.DisplayName.Should().Be("TestCommand");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("boom");
        activity.Events.Should().Contain(e => e.Name == "exception");
        var exceptionEvent = activity.Events.Single(x => x.Name == "exception");
        exceptionEvent.Tags.Should().Contain(x => x.Key == "exception.message" && Equals(x.Value, "boom"));
    }

    [Fact]
    public async Task Handle_LogsFailureAndMarksActivityAsError_WhenHandlerReturnsFailureResult()
    {
        var logger = new TestLogger<LoggingPipelineBehaviour<TestCommand, FlowChatResult<Guid>>>();
        var behaviour = new LoggingPipelineBehaviour<TestCommand, FlowChatResult<Guid>>(logger);
        using var collector = new ActivityCollector();

        var response = await behaviour.Handle(
            new TestCommand(),
            _ => Task.FromResult(FlowChatResult<Guid>.Failure(DomainError.Validation("validation failed", ["Email is required."]))),
            CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.ErrorType.Should().Be(ErrorType.Validation);

        logger.Entries.Should().HaveCount(2);
        logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain("command started: TestCommand");
        logger.Entries[1].LogLevel.Should().Be(LogLevel.Warning);
        logger.Entries[1].Message.Should().Contain("command failed: TestCommand");
        logger.Entries[1].Message.Should().Contain("ErrorType: Validation");

        var activity = collector.Activities.Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("validation failed");
        activity.Tags.Single(x => x.Key == "error.type").Value.Should().Be("validation");
        activity.TagObjects.Single(x => x.Key == "error.count").Value.Should().Be(1);
    }

    private sealed record TestCommand : ICommand<Guid>;

    private sealed class ActivityCollector : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly string _activitySourceName = typeof(TestCommand).Assembly.GetName().Name ?? "FlowChat.Application";

        public ActivityCollector()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == _activitySourceName,
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
