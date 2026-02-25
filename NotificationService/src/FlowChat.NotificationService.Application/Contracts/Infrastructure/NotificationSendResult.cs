namespace FlowChat.NotificationService.Application.Contracts.Infrastructure;

public sealed record NotificationSendResult(
    bool IsSuccess,
    string? ProviderMessageId,
    string? Error);
