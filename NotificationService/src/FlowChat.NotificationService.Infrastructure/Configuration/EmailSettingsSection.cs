using FlowChat.Core.Contracts;

namespace FlowChat.NotificationService.Infrastructure.Configuration;

public sealed class EmailSettingsSection : SettingsSectionBase
{
    public override string SectionName => "EmailSettings";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "FlowChat Notifications";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
