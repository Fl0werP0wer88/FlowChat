using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.Conversation.Public.CopyDuetAsGroup;

public sealed class CopyDuetAsGroupRequest : IServiceInput
{
    public Guid PartnerUserId { get; init; }
}
