using System.Net;
using System.Net.Http;
using System.Security.Claims;
using CSharpFunctionalExtensions;
using FlowChat.Core.Messaging;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Routing;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Application;
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
    public Uri? LastMessageBaseAddress { get; private set; }
    public Uri? LastPresenceBaseAddress { get; private set; }
    public PublishMessageRequest? LastPublishMessageRequest { get; private set; }
    public PublishPresenceChangeRequest? LastPublishPresenceChangeRequest { get; private set; }

    public Task PublishMessageAsync(Uri baseAddress, PublishMessageRequest request, CancellationToken cancellationToken)
    {
        LastMessageBaseAddress = baseAddress;
        LastPublishMessageRequest = request;
        return Task.CompletedTask;
    }

    public Task PublishPresenceChangeAsync(
        Uri baseAddress,
        PublishPresenceChangeRequest request,
        CancellationToken cancellationToken)
    {
        LastPresenceBaseAddress = baseAddress;
        LastPublishPresenceChangeRequest = request;
        return Task.CompletedTask;
    }
}

internal sealed class CapturingRealtimeEventRouter : IRealtimeEventRouter
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

internal sealed class StubRealtimeRoutingTopologyReader : IRealtimeRoutingTopologyReader
{
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> Result { get; set; } =
        new Dictionary<Guid, IReadOnlyCollection<string>>();

    public Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetInstanceIdsByUserAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result);
}

internal sealed class CapturingMediator : IMediator
{
    public object? LastSentRequest { get; private set; }
    public FlowChatResult<Unit> SendUnitResult { get; set; } = FlowChatResult<Unit>.Success(Unit.Value);
    public Exception? SendException { get; set; }

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
        if (SendException is not null)
        {
            throw SendException;
        }

        LastSentRequest = request;
        if (typeof(TResponse) == typeof(FlowChatResult<Unit>))
        {
            return Task.FromResult((TResponse)(object)SendUnitResult);
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
    public IReadOnlyCollection<RealtimeConnectionRefreshEntry>? LastRefreshedConnections { get; private set; }
    public RealtimeConnectionMutationResult? RegisterResult { get; set; }
    public RealtimeConnectionMutationResult? UnregisterResult { get; set; }

    public Exception? RegisterException { get; set; }
    public Exception? UnregisterException { get; set; }

    public Task<RealtimeConnectionMutationResult> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        if (RegisterException is not null)
        {
            throw RegisterException;
        }

        LastRegisteredUserId = userId;
        LastRegisteredConnectionId = connectionId;
        return Task.FromResult(RegisterResult ?? new RealtimeConnectionMutationResult(
            userId,
            connectionId,
            1,
            DateTimeOffset.UtcNow));
    }

    public Task<RealtimeConnectionMutationResult?> UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        LastUnregisteredConnectionId = connectionId;
        if (UnregisterException is not null)
        {
            throw UnregisterException;
        }

        return Task.FromResult(UnregisterResult);
    }

    public Task RefreshAsync(IReadOnlyCollection<RealtimeConnectionRefreshEntry> connections, CancellationToken cancellationToken)
    {
        LastRefreshedConnections = connections;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingIntegrationEventPublisher : IDirectEventPublisher
{
    private readonly List<IntegrationEvent> _published = [];

    public IReadOnlyList<IntegrationEvent> Published => _published.AsReadOnly();
    public Exception? PublishException { get; set; }

    public Task Publish<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        if (PublishException is not null)
        {
            throw PublishException;
        }

        _published.Add(message);
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


