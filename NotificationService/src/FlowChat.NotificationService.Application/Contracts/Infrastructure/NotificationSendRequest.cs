namespace FlowChat.NotificationService.Application.Contracts.Infrastructure;

public sealed record NotificationSendRequest(
    Guid UserId,
    string RecipientEmail,
    string Subject,
    string Body);
