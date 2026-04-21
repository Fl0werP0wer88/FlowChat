using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.ChatMessage.Public.SendChatMessage;

public sealed record SendChatMessageResponse(Guid MessageId) : IServiceOutput;
