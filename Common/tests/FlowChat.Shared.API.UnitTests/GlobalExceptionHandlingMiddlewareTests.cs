using System.Text.Json;
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

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Detail);

        var errorLog = Assert.Single(logger.Entries, entry => entry.LogLevel == LogLevel.Error);
        Assert.Contains("Unhandled exception while processing POST /test. TraceId: trace-123", errorLog.Message);
        Assert.IsType<InvalidOperationException>(errorLog.Exception);
    }

    [Fact]
    public async Task InvokeAsync_LogsWarningAndRethrows_WhenResponseHasStarted()
    {
        var logger = new TestLogger<GlobalExceptionHandlingMiddleware>();
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            logger);
        var context = CreateStartedResponseHttpContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Equal("boom", exception.Message);
        Assert.Single(logger.Entries, entry => entry.LogLevel == LogLevel.Error);
        Assert.Single(logger.Entries, entry => entry.LogLevel == LogLevel.Warning);
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
        var features = new FeatureCollection();
        features.Set<IHttpResponseFeature>(new StartedHttpResponseFeature());

        var context = new DefaultHttpContext(features)
        {
            TraceIdentifier = "trace-123",
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

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

