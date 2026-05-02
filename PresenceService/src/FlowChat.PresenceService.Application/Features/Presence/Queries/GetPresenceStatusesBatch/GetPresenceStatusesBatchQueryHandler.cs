using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.PresenceService.Application.Features.Presence.Queries.GetContactPresenceStatuses;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetPresenceStatusesBatch;

public sealed class GetPresenceStatusesBatchQueryHandler(IPresenceStatusStore presenceStatusStore)
    : IRequestHandler<GetPresenceStatusesBatchQuery, FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>>
{
    private readonly IPresenceStatusStore _presenceStatusStore =
        presenceStatusStore ?? throw new ArgumentNullException(nameof(presenceStatusStore));

    public async Task<FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>> Handle(
        GetPresenceStatusesBatchQuery request,
        CancellationToken cancellationToken)
    {
        var userIds = request.UserIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (userIds.Length == 0)
        {
            return FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>.Success([]);
        }

        var snapshots = await _presenceStatusStore.GetManyAsync(userIds, cancellationToken);

        var result = userIds
            .Select(userId => snapshots.TryGetValue(userId, out var snapshot)
                ? new ContactPresenceStatusDto(snapshot.UserId, snapshot.Status, snapshot.ChangedAtUtc)
                : new ContactPresenceStatusDto(userId, PresenceStatus.Invisible, DateTimeOffset.MinValue))
            .ToArray();

        return FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>.Success(result);
    }
}
