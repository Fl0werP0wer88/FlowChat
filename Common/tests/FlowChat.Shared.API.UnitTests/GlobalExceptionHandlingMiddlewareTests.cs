using System.Text.Json;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace FlowChat.Shared.API.UnitTests;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ReturnsProblemDetailsAndLogsError_WhenDownstreamThrows()
    {
        var logger = new TestLogger<GlobalExceptionHandlingMiddleware>();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith("application/json");
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.Should().Be("An unexpected error occurred.");

        var errorLog = logger.Entries.Should().ContainSingle(entry => entry.LogLevel == LogLevel.Error).Subject;
        errorLog.Message.Should().Contain("Unhandled exception while processing POST /test. TraceId: trace-123");
        errorLog.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task InvokeAsync_ReturnsIsolableFailureKind_WhenDownstreamThrowsIsolableException()
    {
        var logger = new TestLogger<GlobalExceptionHandlingMiddleware>();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new IsolableException("bad batch item"),
            logger);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var document = JsonDocument.Parse(responseBody);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        document.RootElement
            .GetProperty(ProblemDetailsExtensionNames.FailureKind)
            .GetString()
            .Should().Be("Isolable");
    }

    [Fact]
    public async Task InvokeAsync_LogsWarningAndRethrows_WhenResponseHasStarted()
    {
        var logger = new TestLogger<GlobalExceptionHandlingMiddleware>();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            logger);
        var context = CreateStartedResponseHttpContext();

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        logger.Entries.Should().ContainSingle(entry => entry.LogLevel == LogLevel.Error);
        logger.Entries.Should().ContainSingle(entry => entry.LogLevel == LogLevel.Warning);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123",
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static HttpContext CreateStartedResponseHttpContext()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123",
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        context.Features.Set<IHttpResponseFeature>(new StartedHttpResponseFeature());
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/test";

        return context;
    }

    private sealed class SingleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType.IsInstanceOfType(service) ? service : null;
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            return new ValidationProblemDetails(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
        }
    }

    private sealed class StartedHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted => true;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
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
