using System.Net;
using System.Net.Http;
using System.Security.Claims;
using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.Shared.Domain;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

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
        if (typeof(TResponse) == typeof(FlowChatResult<Unit>))
        {
            var success = FlowChatResult<Unit>.Success(Unit.Value);
            return Task.FromResult((TResponse)(object)success);
        }

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

internal sealed class CapturingRealtimeConnectionRegistry : IRealtimeConnectionRegistry
{
    public Guid? LastRegisteredUserId { get; private set; }
    public string? LastRegisteredConnectionId { get; private set; }
    public string? LastUnregisteredConnectionId { get; private set; }
    public IReadOnlyCollection<string>? LastRefreshedConnectionIds { get; private set; }

    public Exception? RegisterException { get; set; }
    public Exception? UnregisterException { get; set; }

    public Task RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        if (RegisterException is not null)
        {
            throw RegisterException;
        }

        LastRegisteredUserId = userId;
        LastRegisteredConnectionId = connectionId;
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        LastUnregisteredConnectionId = connectionId;
        if (UnregisterException is not null)
        {
            throw UnregisterException;
        }

        return Task.CompletedTask;
    }

    public Task RefreshAsync(IReadOnlyCollection<string> connectionIds, CancellationToken cancellationToken)
    {
        LastRefreshedConnectionIds = connectionIds;
        return Task.CompletedTask;
    }
}

internal sealed class CapturingGroupManager : IGroupManager
{
    public List<(string ConnectionId, string GroupName)> AddedConnections { get; } = [];
    public List<(string ConnectionId, string GroupName)> RemovedConnections { get; } = [];

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        AddedConnections.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        RemovedConnections.Add((connectionId, groupName));
        return Task.CompletedTask;
    }
}

internal sealed class TestHubCallerContext : HubCallerContext
{
    private readonly CancellationTokenSource _connectionAbortedSource = new();

    public TestHubCallerContext(string connectionId, ClaimsPrincipal? user = null)
    {
        ConnectionId = connectionId;
        User = user;
        Items = new Dictionary<object, object?>();
        Features = new FeatureCollection();
    }

    public bool AbortCalled { get; private set; }

    public override string ConnectionId { get; }

    public override string? UserIdentifier => User?.FindFirstValue("sub") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public override ClaimsPrincipal? User { get; }

    public override IDictionary<object, object?> Items { get; }

    public override IFeatureCollection Features { get; }

    public override CancellationToken ConnectionAborted => _connectionAbortedSource.Token;

    public override void Abort()
    {
        AbortCalled = true;
        _connectionAbortedSource.Cancel();
    }
}


