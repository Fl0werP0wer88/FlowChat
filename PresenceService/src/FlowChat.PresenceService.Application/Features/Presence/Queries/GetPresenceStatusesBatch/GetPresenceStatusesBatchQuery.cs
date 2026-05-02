using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Features.Presence.Queries.GetContactPresenceStatuses;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetPresenceStatusesBatch;

public sealed record GetPresenceStatusesBatchQuery(IReadOnlyCollection<Guid> UserIds)
    : IRequest<FlowChatResult<IReadOnlyCollection<ContactPresenceStatusDto>>>;
