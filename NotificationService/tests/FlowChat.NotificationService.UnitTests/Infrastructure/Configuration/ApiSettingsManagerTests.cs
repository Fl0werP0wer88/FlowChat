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

    // --- GetApiRuntimeSettings ---

    [Fact]
    public void GetApiRuntimeSettings_WhenValuesConfigured_ReturnsConfiguredSettings()
    {
        var config = BuildConfiguration(new()
        {
            ["ApiUrl"] = "https://api.flowchat.com",
            ["BlazorUrl"] = "https://app.flowchat.com"
        });

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetApiRuntimeSettings();

        settings.ApiUrl.Should().Be("https://api.flowchat.com");
        settings.BlazorUrl.Should().Be("https://app.flowchat.com");
    }

    [Fact]
    public void GetApiRuntimeSettings_WhenNoConfiguration_ReturnsDefaultValues()
    {
        var config = BuildConfiguration([]);

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetApiRuntimeSettings();

        settings.ApiUrl.Should().Be("https://localhost:5000");
        settings.BlazorUrl.Should().Be("https://localhost:5010");
    }

    [Fact]
    public void GetApiRuntimeSettings_TopLevelApiUrlOverridesFlowChatSection()
    {
        var config = BuildConfiguration(new()
        {
            ["FlowChat:ApiUrl"] = "https://section-api.flowchat.com",
            ["ApiUrl"] = "https://toplevel-api.flowchat.com"
        });

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetApiRuntimeSettings();

        settings.ApiUrl.Should().Be("https://toplevel-api.flowchat.com");
    }

    // --- GetEmailSettings ---

    [Fact]
    public void GetEmailSettings_WhenConfigured_ReturnsConfiguredValues()
    {
        var config = BuildConfiguration(new()
        {
            [$"{EmailSettings.SectionName}:SmtpHost"] = "smtp.mailhog.local",
            [$"{EmailSettings.SectionName}:SmtpPort"] = "1025",
            [$"{EmailSettings.SectionName}:FromEmail"] = "noreply@flowchat.com",
            [$"{EmailSettings.SectionName}:FromName"] = "FlowChat Bot",
            [$"{EmailSettings.SectionName}:Username"] = "user",
            [$"{EmailSettings.SectionName}:Password"] = "pass",
            [$"{EmailSettings.SectionName}:EnableSsl"] = "false"
        });

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetEmailSettings();

        settings.SmtpHost.Should().Be("smtp.mailhog.local");
        settings.SmtpPort.Should().Be(1025);
        settings.FromEmail.Should().Be("noreply@flowchat.com");
        settings.FromName.Should().Be("FlowChat Bot");
        settings.Username.Should().Be("user");
        settings.Password.Should().Be("pass");
        settings.EnableSsl.Should().BeFalse();
    }

    [Fact]
    public void GetEmailSettings_WhenSectionMissing_ReturnsDefaultValues()
    {
        var config = BuildConfiguration([]);

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetEmailSettings();

        settings.SmtpHost.Should().BeEmpty();
        settings.SmtpPort.Should().Be(587);
        settings.FromName.Should().Be("FlowChat Notifications");
        settings.EnableSsl.Should().BeTrue();
    }

    // --- GetInternalApiSettings ---

    [Fact]
    public void GetInternalApiSettings_WhenConfigured_ReturnsApiKey()
    {
        var config = BuildConfiguration(new()
        {
            [$"{InternalApiSettings.SectionName}:ApiKey"] = "super-secret-key"
        });

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetInternalApiSettings();

        settings.ApiKey.Should().Be("super-secret-key");
    }

    [Fact]
    public void GetInternalApiSettings_WhenSectionMissing_ReturnsDefaultEmptyApiKey()
    {
        var config = BuildConfiguration([]);

        var manager = new ApiSettingsManager(config);
        var settings = manager.GetInternalApiSettings();

        settings.ApiKey.Should().BeEmpty();
    }
}
