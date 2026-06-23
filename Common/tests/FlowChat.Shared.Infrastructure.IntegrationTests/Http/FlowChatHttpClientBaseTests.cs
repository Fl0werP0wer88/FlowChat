using System.Net;
using System.Net.Http.Json;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Http;
using FlowChat.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using FluentAssertions;

namespace FlowChat.Shared.Infrastructure.IntegrationTests.Http;

public sealed class FlowChatHttpClientBaseTests
{
    [Fact]
    public async Task SendAsync_WhenApiReturnsAccepted_CompletesWithoutException()
    {
        string? requestBody = null;

        using var host = await CreateHostAsync(async context =>
        {
            requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
            context.Response.StatusCode = StatusCodes.Status202Accepted;
        });

        var client = new TestConsumerHttpClient(host.GetTestClient());

        var act = () => client.SendPingAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();

        requestBody.Should().Contain("ping");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsBadRequest_ThrowsNonTransientExceptionWithFailureMessage()
    {
        using var host = await CreateHostAsync(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(" invalid payload ", context.RequestAborted);
        });

        var client = new TestConsumerHttpClient(host.GetTestClient());

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.SendPingAsync(CancellationToken.None));

        exception.Message.Should().Be("Test Consumer API returned 400 Bad Request: invalid payload");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsTransientProblemDetails_ThrowsTransientException()
    {
        using var host = await CreateHostAsync(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                $$"""{"detail":"conflict","{{ProblemDetailsExtensionNames.IsTransient}}":true}""",
                context.RequestAborted);
        });

        var client = new TestConsumerHttpClient(host.GetTestClient());

        var exception = await Assert.ThrowsAsync<TransientException>(() =>
            client.SendPingAsync(CancellationToken.None));

        exception.Message.Should().Contain("500");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsProblemDetailsWithFalseTransientFlag_ThrowsNonTransientException()
    {
        using var host = await CreateHostAsync(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                $$"""{"detail":"conflict","{{ProblemDetailsExtensionNames.IsTransient}}":false}""",
                context.RequestAborted);
        });

        var client = new TestConsumerHttpClient(host.GetTestClient());

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.SendPingAsync(CancellationToken.None));

        exception.Message.Should().Be(
            $$"""Test Consumer API returned 409 Conflict: {"detail":"conflict","{{ProblemDetailsExtensionNames.IsTransient}}":false}""");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsInvalidProblemDetailsPayload_ThrowsNonTransientException()
    {
        using var host = await CreateHostAsync(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync("{not-json", context.RequestAborted);
        });

        var client = new TestConsumerHttpClient(host.GetTestClient());

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.SendPingAsync(CancellationToken.None));

        exception.Message.Should().Be("Test Consumer API returned 500 Internal Server Error: {not-json");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsPayloadWithoutTransientFlag_ThrowsNonTransientException()
    {
        using var host = await CreateHostAsync(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync("""{"detail":"not found"}""", context.RequestAborted);
        });

        var client = new TestConsumerHttpClient(host.GetTestClient());

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.SendPingAsync(CancellationToken.None));

        exception.Message.Should().Be("""Test Consumer API returned 404 Not Found: {"detail":"not found"}""");
    }

    [Fact]
    public async Task SendAsync_WhenNonTransientResponseHasEmptyBody_OmitsBodySuffix()
    {
        using var host = await CreateHostAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });

        var client = new TestConsumerHttpClient(host.GetTestClient());

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.SendPingAsync(CancellationToken.None));

        exception.Message.Should().Be("Test Consumer API returned 401 Unauthorized");
    }

    private static async Task<IHost> CreateHostAsync(RequestDelegate requestDelegate)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseTestServer();
                webHostBuilder.Configure(app => app.Run(requestDelegate));
            });

        var host = await builder.StartAsync();

        return host;
    }

    private sealed class TestConsumerHttpClient(HttpClient httpClient)
        : FlowChatHttpClientBase(httpClient)
    {
        protected override string ClientDisplayName => "Test Consumer API";

        public async Task SendPingAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/test")
            {
                Content = JsonContent.Create(new PingRequest("ping"))
            };

            await SendAsync(request, cancellationToken);
        }
    }

    private sealed record PingRequest(string Value);
}

