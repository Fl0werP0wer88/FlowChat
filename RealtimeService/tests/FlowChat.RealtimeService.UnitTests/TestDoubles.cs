using System.Net;
using System.Net.Http;
using System.Security.Claims;
using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Application;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace FlowChat.RealtimeService.UnitTests;

internal sealed class CapturingRealtimeClientDispatcher : IRealtimeClientDispatcher
{
    public ChatMessageParam? LastMessageNotification { get; private set; }
    public PresenceChangedParam? LastPresenceNotification { get; private set; }
    public GroupConversationChangedParam? LastGroupConversationChangedNotification { get; private set; }
    public GroupConversationParticipantsAddedParam? LastGroupConversationParticipantsAddedNotification { get; private set; }
    public GroupConversationParticipantsRemovedParam? LastGroupConversationParticipantsRemovedNotification { get; private set; }

    public Task MessageReceivedAsync(ChatMessageParam notification, CancellationToken cancellationToken)
    {
        LastMessageNotification = notification;
        return Task.CompletedTask;
    }

    public Task PresenceChangedAsync(PresenceChangedParam notification, CancellationToken cancellationToken)
    {
        LastPresenceNotification = notification;
        return Task.CompletedTask;
    }

    public Task GroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken)
    {
        LastGroupConversationChangedNotification = notification;
        return Task.CompletedTask;
    }

    public Task GroupConversationParticipantsAddedAsync(GroupConversationParticipantsAddedParam notification, CancellationToken cancellationToken)
    {
        LastGroupConversationParticipantsAddedNotification = notification;
        return Task.CompletedTask;
    }

    public Task GroupConversationParticipantsRemovedAsync(GroupConversationParticipantsRemovedParam notification, CancellationToken cancellationToken)
    {
        LastGroupConversationParticipantsRemovedNotification = notification;
        return Task.CompletedTask;
    }
}

internal sealed class CapturingRealtimeEventRouter : IRealtimeEventRouter
{
    public ChatMessageParam? LastMessageNotification { get; private set; }
    public PresenceChangedParam? LastPresenceNotification { get; private set; }
    public GroupConversationChangedParam? LastGroupConversationChangedNotification { get; private set; }
    public GroupConversationParticipantsAddedParam? LastGroupConversationParticipantsAddedNotification { get; private set; }
    public GroupConversationParticipantsRemovedParam? LastGroupConversationParticipantsRemovedNotification { get; private set; }
    public DuetConversationCreatedParam? LastDuetConversationCreatedNotification { get; private set; }

    public Task RouteMessageAsync(ChatMessageParam notification, CancellationToken cancellationToken)
    {
        LastMessageNotification = notification;
        return Task.CompletedTask;
    }

    public Task RoutePresenceChangeAsync(PresenceChangedParam notification, CancellationToken cancellationToken)
    {
        LastPresenceNotification = notification;
        return Task.CompletedTask;
    }

    public Task RouteGroupConversationChangedAsync(GroupConversationChangedParam notification, CancellationToken cancellationToken)
    {
        LastGroupConversationChangedNotification = notification;
        return Task.CompletedTask;
    }

    public Task RouteGroupConversationParticipantsAddedAsync(GroupConversationParticipantsAddedParam notification, CancellationToken cancellationToken)
    {
        LastGroupConversationParticipantsAddedNotification = notification;
        return Task.CompletedTask;
    }

    public Task RouteGroupConversationParticipantsRemovedAsync(GroupConversationParticipantsRemovedParam notification, CancellationToken cancellationToken)
    {
        LastGroupConversationParticipantsRemovedNotification = notification;
        return Task.CompletedTask;
    }

    public Task RouteDuetConversationCreatedAsync(DuetConversationCreatedParam notification, CancellationToken cancellationToken)
    {
        LastDuetConversationCreatedNotification = notification;
        return Task.CompletedTask;
    }
}

internal sealed class StubUserInstanceRoutingReader : IUserInstanceRoutingReader
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
    private readonly List<object> _sentRequests = [];

    public object? LastSentRequest { get; private set; }
    public IReadOnlyList<object> SentRequests => _sentRequests.AsReadOnly();
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
        _sentRequests.Add(request!);
        return Task.CompletedTask;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (SendException is not null)
        {
            throw SendException;
        }

        LastSentRequest = request;
        _sentRequests.Add(request);
        if (typeof(TResponse) == typeof(FlowChatResult<Unit>))
        {
            return Task.FromResult((TResponse)(object)SendUnitResult);
        }

        return Task.FromResult(default(TResponse)!);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        LastSentRequest = request;
        _sentRequests.Add(request);
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
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
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
    public Dictionary<Guid, IReadOnlyCollection<string>> ConnectionIdsByUserId { get; } = [];

    public Exception? RegisterException { get; set; }
    public Exception? UnregisterException { get; set; }

    public Task<RealtimeConnectionMutationResult> RegisterAsync(Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        if (RegisterException is not null)
        {
            return Task.FromException<RealtimeConnectionMutationResult>(RegisterException);
        }

        LastRegisteredUserId = userId;
        LastRegisteredConnectionId = connectionId;
        return Task.FromResult(RegisterResult ?? new RealtimeConnectionMutationResult(
            userId,
            connectionId,
            1,
            true,
            false,
            DateTimeOffset.UtcNow));
    }

    public Task<RealtimeConnectionMutationResult?> UnregisterAsync(string connectionId, CancellationToken cancellationToken)
    {
        LastUnregisteredConnectionId = connectionId;
        if (UnregisterException is not null)
        {
            return Task.FromException<RealtimeConnectionMutationResult?>(UnregisterException);
        }

        return Task.FromResult(UnregisterResult);
    }

    public Task RefreshAsync(IReadOnlyCollection<RealtimeConnectionRefreshEntry> connections, CancellationToken cancellationToken)
    {
        LastRefreshedConnections = connections;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> GetConnectionIdsByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> result = userIds
            .Where(ConnectionIdsByUserId.ContainsKey)
            .ToDictionary(userId => userId, userId => ConnectionIdsByUserId[userId]);

        return Task.FromResult(result);
    }
}

internal sealed class CapturingPresenceInternalApiClient : IPresenceInternalApiClient
{
    public Guid? LastInitializePresenceStatusUserId { get; private set; }
    public Exception? InitializePresenceStatusException { get; set; }
    public Guid? LastDeletePresenceStatusUserId { get; private set; }
    public Exception? DeletePresenceStatusException { get; set; }

    public Task InitializePresenceStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (InitializePresenceStatusException is not null)
        {
            return Task.FromException(InitializePresenceStatusException);
        }

        LastInitializePresenceStatusUserId = userId;
        return Task.CompletedTask;
    }

    public Task DeletePresenceStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (DeletePresenceStatusException is not null)
        {
            return Task.FromException(DeletePresenceStatusException);
        }

        LastDeletePresenceStatusUserId = userId;
        return Task.CompletedTask;
    }

    public Task RefreshPresenceStatusAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class CapturingRealtimeGroupManager : IRealtimeGroupManager
{
    public List<(string ConnectionId, Guid UserId)> AddedToUserGroup { get; } = [];
    public List<(string ConnectionId, Guid UserId)> RemovedFromUserGroup { get; } = [];
    public List<(string ConnectionId, Guid ConversationId)> AddedToConversationGroup { get; } = [];
    public List<(string ConnectionId, Guid ConversationId)> RemovedFromConversationGroup { get; } = [];

    public Exception? AddToUserGroupException { get; set; }
    public Exception? RemoveFromUserGroupException { get; set; }
    public Exception? AddToConversationGroupException { get; set; }
    public Exception? RemoveFromConversationGroupException { get; set; }

    public Task AddToUserGroupAsync(string connectionId, Guid userId, CancellationToken cancellationToken)
    {
        if (AddToUserGroupException is not null)
        {
            return Task.FromException(AddToUserGroupException);
        }

        AddedToUserGroup.Add((connectionId, userId));
        return Task.CompletedTask;
    }

    public Task RemoveFromUserGroupAsync(string connectionId, Guid userId, CancellationToken cancellationToken)
    {
        if (RemoveFromUserGroupException is not null)
        {
            return Task.FromException(RemoveFromUserGroupException);
        }

        RemovedFromUserGroup.Add((connectionId, userId));
        return Task.CompletedTask;
    }

    public Task AddToConversationGroupAsync(string connectionId, Guid conversationId, CancellationToken cancellationToken)
    {
        if (AddToConversationGroupException is not null)
        {
            return Task.FromException(AddToConversationGroupException);
        }

        AddedToConversationGroup.Add((connectionId, conversationId));
        return Task.CompletedTask;
    }

    public Task RemoveFromConversationGroupAsync(string connectionId, Guid conversationId, CancellationToken cancellationToken)
    {
        if (RemoveFromConversationGroupException is not null)
        {
            return Task.FromException(RemoveFromConversationGroupException);
        }

        RemovedFromConversationGroup.Add((connectionId, conversationId));
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


