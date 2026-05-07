using FlowChat.NotificationService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.NotificationService.UnitTests.Infrastructure.Configuration;

public sealed class ApiSettingsManagerTests
{
    private static IOptions<T> BuildOptions<T>(Dictionary<string, string?> values)
        where T : class, new()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSettingsSections(config, typeof(T).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IOptions<T>>();
    }

    [Fact]
    public void GetEmailSettingsSection_WhenConfigured_ReturnsConfiguredValues()
    {
        var settings = BuildOptions<EmailSettingsSection>(new()
        {
            [$"{new EmailSettingsSection().SectionName}:SmtpHost"] = "smtp.mailhog.local",
            [$"{new EmailSettingsSection().SectionName}:SmtpPort"] = "1025",
            [$"{new EmailSettingsSection().SectionName}:FromEmail"] = "noreply@flowchat.com",
            [$"{new EmailSettingsSection().SectionName}:FromName"] = "FlowChat Bot",
            [$"{new EmailSettingsSection().SectionName}:Username"] = "user",
            [$"{new EmailSettingsSection().SectionName}:Password"] = "pass",
            [$"{new EmailSettingsSection().SectionName}:EnableSsl"] = "false"
        }).Value;

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
        var settings = BuildOptions<EmailSettingsSection>([]).Value;

        settings.SmtpHost.Should().BeEmpty();
        settings.SmtpPort.Should().Be(587);
        settings.FromName.Should().Be("FlowChat Notifications");
        settings.EnableSsl.Should().BeTrue();
    }

    [Fact]
    public void GetInternalApiSettingsSection_WhenConfigured_ReturnsApiKey()
    {
        var settings = BuildOptions<InternalApiSettingsSection>(new()
        {
            [$"{new InternalApiSettingsSection().SectionName}:ApiKey"] = "super-secret-key"
        }).Value;

        settings.ApiKey.Should().Be("super-secret-key");
    }

    [Fact]
    public void GetInternalApiSettingsSection_WhenSectionMissing_ReturnsDefaultEmptyApiKey()
    {
        var settings = BuildOptions<InternalApiSettingsSection>([]).Value;

        settings.ApiKey.Should().BeEmpty();
    }
}
