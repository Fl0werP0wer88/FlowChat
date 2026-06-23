using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetContactPresenceStatuses;

public sealed record GetContactPresenceStatusesQuery(Guid ObserverUserId)
    : IRequest<FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>>;
