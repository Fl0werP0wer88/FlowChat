using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetContactPresenceStatuses;

public sealed class GetContactPresenceStatusesQueryHandler(
    IContactObserverProjectionReadRepository contactObserverProjectionReadRepository,
    IPresenceStatusStore presenceStatusStore)
    : IRequestHandler<GetContactPresenceStatusesQuery, FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>>
{
    private readonly IContactObserverProjectionReadRepository _contactObserverProjectionReadRepository =
        contactObserverProjectionReadRepository ?? throw new ArgumentNullException(nameof(contactObserverProjectionReadRepository));
    private readonly IPresenceStatusStore _presenceStatusStore =
        presenceStatusStore ?? throw new ArgumentNullException(nameof(presenceStatusStore));

    public async Task<FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>> Handle(
        GetContactPresenceStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var observedUserIds = await _contactObserverProjectionReadRepository
            .GetObservedUserIdsAsync(request.ObserverUserId, cancellationToken);

        if (observedUserIds.Count == 0)
        {
            return FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>.Success([]);
        }

        var snapshots = await _presenceStatusStore.GetManyAsync(observedUserIds, cancellationToken);

        var result = observedUserIds
            .Select(userId => snapshots.TryGetValue(userId, out var snapshot)
                ? new ContactPresenceStatusDto(snapshot.UserId, snapshot.Status, snapshot.ChangedAtUtc)
                : new ContactPresenceStatusDto(userId, PresenceStatus.Invisible, DateTimeOffset.MinValue))
            .ToArray();

        return FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>.Success(result);
    }
}
