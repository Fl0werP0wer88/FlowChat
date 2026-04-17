using FlowChat.UserProfileService.Infrastructure.Configuration;
using FlowChat.UserProfileService.Infrastructure.Services;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class EmailVerificationLinkBuilderTests
{
    private readonly Mock<IApiSettingsManager> _apiSettingsManagerMock = new();

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlHasNoQuery_AppendsTokenQueryParameter()
    {
        _apiSettingsManagerMock
            .Setup(x => x.GetConfirmationLinksSettingsSection())
            .Returns(new ConfirmationLinksSettingsSection
            {
                EmailVerificationBaseUrl = "https://frontend.flowchat.local/verify"
            });

        var sut = new EmailVerificationLinkBuilder(_apiSettingsManagerMock.Object);

        var result = sut.BuildEmailVerificationLink("token with spaces/+");

        result.Should().Be("https://frontend.flowchat.local/verify?token=token%20with%20spaces%2F%2B");
    }

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlAlreadyHasQuery_AppendsTokenWithAmpersand()
    {
        _apiSettingsManagerMock
            .Setup(x => x.GetConfirmationLinksSettingsSection())
            .Returns(new ConfirmationLinksSettingsSection
            {
                EmailVerificationBaseUrl = "https://frontend.flowchat.local/verify?source=email"
            });

        var sut = new EmailVerificationLinkBuilder(_apiSettingsManagerMock.Object);

        var result = sut.BuildEmailVerificationLink("token-123");

        result.Should().Be("https://frontend.flowchat.local/verify?source=email&token=token-123");
    }

    [Fact]
    public void BuildEmailVerificationLink_WhenBaseUrlIsMissing_ThrowsInvalidOperationException()
    {
        _apiSettingsManagerMock
            .Setup(x => x.GetConfirmationLinksSettingsSection())
            .Returns(new ConfirmationLinksSettingsSection());

        var sut = new EmailVerificationLinkBuilder(_apiSettingsManagerMock.Object);

        var act = () => sut.BuildEmailVerificationLink("token-123");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Missing configuration value: ConfirmationLinks:EmailVerificationBaseUrl.");
    }
}
