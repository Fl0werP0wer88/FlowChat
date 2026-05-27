using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;

public sealed record BulkUpsertUserProfileProjectionResponse(
    int RequestedCount,
    int UpsertedCount) : IServiceOutput;
