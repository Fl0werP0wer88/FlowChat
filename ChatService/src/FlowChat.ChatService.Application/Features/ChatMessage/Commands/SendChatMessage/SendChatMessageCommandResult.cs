namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed record SendChatMessageCommandResult(Guid MessageId, DateTimeOffset SentAtUtc);
