namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed record SendChatMessageCommandResultV2(
    Guid MessageId,
    DateTimeOffset SentAtUtc,
    long SequenceNum);
