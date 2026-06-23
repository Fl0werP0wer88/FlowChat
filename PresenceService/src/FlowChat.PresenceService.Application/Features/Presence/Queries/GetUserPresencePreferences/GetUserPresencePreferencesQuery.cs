using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetUserPresencePreferences;

public sealed record GetUserPresencePreferencesQuery(Guid UserId)
    : IRequest<FlowChatResult<PresenceStatus?>>;
