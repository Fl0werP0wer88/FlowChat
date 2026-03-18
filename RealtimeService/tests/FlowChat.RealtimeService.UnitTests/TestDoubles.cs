using System.Net;
using System.Net.Http;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Realtime.Contracts;
using FlowChat.RealtimeService.Domain.Notifications;
using FlowChat.RealtimeService.Infrastructure.Services;
using MediatR;

namespace FlowChat.RealtimeService.UnitTests;

internal sealed class CapturingRealtimeClientDispatcher : IRealtimeClientDispatcher
{
    public ChatMessageNotification? LastMessageNotification { get; private set; }
    public PresenceChangedNotification? LastPresenceNotification { get; private set; }

    public Task ReceiveMessageAsync(ChatMessageNotification notification, CancellationToken cancellationToken)
    {
        LastMessageNotification = notification;
        return Task.CompletedTask;
    }

    public Task PresenceChangedAsync(PresenceChangedNotification notification, CancellationToken cancellationToken)
    {
        LastPresenceNotification = notification;
        return Task.CompletedTask;
    }
}

internal sealed class CapturingRealtimeInternalApiClient : IRealtimeInternalApiClient
{
    public PublishMessageRequest? LastPublishMessageRequest { get; private set; }
    public PublishPresenceChangeRequest? LastPublishPresenceChangeRequest { get; private set; }

    public Task PublishMessageAsync(PublishMessageRequest request, CancellationToken cancellationToken)
    {
        LastPublishMessageRequest = request;
        return Task.CompletedTask;
    }

    public Task PublishPresenceChangeAsync(PublishPresenceChangeRequest request, CancellationToken cancellationToken)
    {
        LastPublishPresenceChangeRequest = request;
        return Task.CompletedTask;
    }
}

internal sealed class CapturingMediator : IMediator
{
    public object? LastSentRequest { get; private set; }

    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification =>
        Task.CompletedTask;

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        LastSentRequest = request;
        return Task.CompletedTask;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        LastSentRequest = request;
        return Task.FromResult(default(TResponse)!);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        LastSentRequest = request;
        return Task.FromResult<object?>(null);
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        AsyncEnumerable.Empty<object?>();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default) =>
        AsyncEnumerable.Empty<TResponse>();
}

internal sealed class CapturingHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
    : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFactory =
        responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return await _responseFactory(request, cancellationToken);
    }
}

internal static class RepositoryPathHelper
{
    public static string GetRepositoryPath(string relativePath)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, relativePath);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}' starting from '{AppContext.BaseDirectory}'.");
    }
}
