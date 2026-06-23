using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using FlowChat.UserProfileService.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationLinkBuilderTests
{
    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlHasNoQuery_AppendsTokenQueryParameter()
    {
        var sut = new EmailVerificationLinkBuilder(Options.Create(new ConfirmationLinksSettingsSection
        {
            EmailVerificationBaseUrl = "https://frontend.flowchat.local/verify"
        }));

        var result = sut.BuildEmailVerificationLink("token with spaces/+");

        result.Should().Be("https://frontend.flowchat.local/verify?token=token%20with%20spaces%2F%2B");
    }

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlAlreadyHasQuery_AppendsTokenWithAmpersand()
    {
        var sut = new EmailVerificationLinkBuilder(Options.Create(new ConfirmationLinksSettingsSection
        {
            EmailVerificationBaseUrl = "https://frontend.flowchat.local/verify?source=email"
        }));

        var result = sut.BuildEmailVerificationLink("token-123");

        result.Should().Be("https://frontend.flowchat.local/verify?source=email&token=token-123");
    }

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlIsMissing_ThrowsInvalidOperationException()
    {
        var sut = new EmailVerificationLinkBuilder(Options.Create(new ConfirmationLinksSettingsSection()));

        var act = () => sut.BuildEmailVerificationLink("token-123");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Missing configuration value: ConfirmationLinks:EmailVerificationBaseUrl.");
    }
}
