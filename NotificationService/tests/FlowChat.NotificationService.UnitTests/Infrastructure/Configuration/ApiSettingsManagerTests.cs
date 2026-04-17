using FlowChat.NotificationService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.NotificationService.UnitTests.Infrastructure.Configuration;

public sealed class ApiSettingsManagerTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    // --- GetEmailSettingsSection ---

    [Fact]
    public void GetEmailSettingsSection_WhenConfigured_ReturnsConfiguredValues()
    {
        var config = BuildConfiguration(new()
        {
            [$"{new EmailSettingsSection().SectionName}:SmtpHost"] = "smtp.mailhog.local",
            [$"{new EmailSettingsSection().SectionName}:SmtpPort"] = "1025",
            [$"{new EmailSettingsSection().SectionName}:FromEmail"] = "noreply@flowchat.com",
            [$"{new EmailSettingsSection().SectionName}:FromName"] = "FlowChat Bot",
            [$"{new EmailSettingsSection().SectionName}:Username"] = "user",
            [$"{new EmailSettingsSection().SectionName}:Password"] = "pass",
            [$"{new EmailSettingsSection().SectionName}:EnableSsl"] = "false"
        });

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetEmailSettingsSection();

        settings.SmtpHost.Should().Be("smtp.mailhog.local");
        settings.SmtpPort.Should().Be(1025);
        settings.FromEmail.Should().Be("noreply@flowchat.com");
        settings.FromName.Should().Be("FlowChat Bot");
        settings.Username.Should().Be("user");
        settings.Password.Should().Be("pass");
        settings.EnableSsl.Should().BeFalse();
    }

    [Fact]
    public void GetEmailSettingsSection_WhenSectionMissing_ReturnsDefaultValues()
    {
        var config = BuildConfiguration([]);

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetEmailSettingsSection();

        settings.SmtpHost.Should().BeEmpty();
        settings.SmtpPort.Should().Be(587);
        settings.FromName.Should().Be("FlowChat Notifications");
        settings.EnableSsl.Should().BeTrue();
    }

    // --- GetInternalApiSettingsSection ---

    [Fact]
    public void GetInternalApiSettingsSection_WhenConfigured_ReturnsApiKey()
    {
        var config = BuildConfiguration(new()
        {
            [$"{new InternalApiSettingsSection().SectionName}:ApiKey"] = "super-secret-key"
        });

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetInternalApiSettingsSection();

        settings.ApiKey.Should().Be("super-secret-key");
    }

    [Fact]
    public void GetInternalApiSettingsSection_WhenSectionMissing_ReturnsDefaultEmptyApiKey()
    {
        var config = BuildConfiguration([]);

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetInternalApiSettingsSection();

        settings.ApiKey.Should().BeEmpty();
    }
}
