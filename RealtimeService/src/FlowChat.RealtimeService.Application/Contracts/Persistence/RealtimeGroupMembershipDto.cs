using FlowChat.RealtimeService.Domain.Enums;

namespace FlowChat.RealtimeService.Application.Contracts.Persistence;

public sealed record RealtimeGroupMembershipDto(
    Guid UserId,
    RealtimeGroupType GroupType,
    Guid ResourceId,
    DateTimeOffset CreatedAt);
