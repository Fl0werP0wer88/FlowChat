using FlowChat.Core.Contracts;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using FlowChat.UserProfileService.Infrastructure.Services;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationLinkBuilderTests
{
    private readonly Mock<ISettingsProvider> _settingsProviderMock = new();

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlHasNoQuery_AppendsTokenQueryParameter()
    {
        _settingsProviderMock
            .Setup(x => x.GetSection<ConfirmationLinksSettingsSection>())
            .Returns(new ConfirmationLinksSettingsSection
            {
                EmailVerificationBaseUrl = "https://frontend.flowchat.local/verify"
            });

        var sut = new EmailVerificationLinkBuilder(_settingsProviderMock.Object);

        var result = sut.BuildEmailVerificationLink("token with spaces/+");

        result.Should().Be("https://frontend.flowchat.local/verify?token=token%20with%20spaces%2F%2B");
    }

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlAlreadyHasQuery_AppendsTokenWithAmpersand()
    {
        _settingsProviderMock
            .Setup(x => x.GetSection<ConfirmationLinksSettingsSection>())
            .Returns(new ConfirmationLinksSettingsSection
            {
                EmailVerificationBaseUrl = "https://frontend.flowchat.local/verify?source=email"
            });

        var sut = new EmailVerificationLinkBuilder(_settingsProviderMock.Object);

        var result = sut.BuildEmailVerificationLink("token-123");

        result.Should().Be("https://frontend.flowchat.local/verify?source=email&token=token-123");
    }

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlIsMissing_ThrowsInvalidOperationException()
    {
        _settingsProviderMock
            .Setup(x => x.GetSection<ConfirmationLinksSettingsSection>())
            .Returns(new ConfirmationLinksSettingsSection());

        var sut = new EmailVerificationLinkBuilder(_settingsProviderMock.Object);

        var act = () => sut.BuildEmailVerificationLink("token-123");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Missing configuration value: ConfirmationLinks:EmailVerificationBaseUrl.");
    }
}
