using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;

public sealed record BulkUpsertOrDeleteUserProfileProjectionResponse(
    int RequestedCount,
    int UpsertedCount,
    int DeletedCount) : IServiceOutput;
