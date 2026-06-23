using FlowChat.Core.Contracts;

namespace FlowChat.NotificationService.Api.Features.Notification.Internal.ProcessUserEmailVerificationRequested;

public sealed class ProcessUserEmailVerificationRequestedRequest : IServiceInput
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ConfirmationLink { get; set; } = string.Empty;

    public string? SourceMessageKey { get; set; }
}
