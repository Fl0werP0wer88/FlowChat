using FlowChat.RealtimeService.Domain.Enums;

namespace FlowChat.RealtimeService.Application.Contracts.Persistence;

public sealed record RealtimeGroupMembershipReadModelDto(
    Guid UserId,
    RealtimeGroupType GroupType,
    Guid ResourceId,
    DateTimeOffset CreatedAt);
