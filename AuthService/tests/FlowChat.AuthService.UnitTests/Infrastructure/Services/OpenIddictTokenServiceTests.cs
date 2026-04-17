using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Core.Contracts;
using FluentAssertions;
using Moq;
using OpenIddict.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class OpenIddictTokenServiceTests
{
    [Fact]
    public void CreatePrincipal_IncludesRequiredClaimsAndScopes()
    {
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<JwtSettingsSection>())
            .Returns(new JwtSettingsSection
            {
                Audience = "FlowChat.Client"
            });

        var sut = new OpenIddictTokenService(settingsProviderMock.Object);

        var principal = sut.CreatePrincipal(
            new AuthenticatedAccount
            {
                Id = Guid.NewGuid(),
                FriendlyUserId = "flower",
                Email = "flower@example.com",
                Roles = ["Admin"]
            },
            ["custom_scope"]);

        principal.FindFirst(OpenIddictConstants.Claims.Subject)!.Value.Should().NotBeNullOrWhiteSpace();
        principal.FindFirst(OpenIddictConstants.Claims.Email)!.Value.Should().Be("flower@example.com");
        principal.FindFirst(OpenIddictConstants.Claims.PreferredUsername)!.Value.Should().Be("flower");
        principal.FindFirst(JwtRegisteredClaimNames.UniqueName)!.Value.Should().Be("flower");
        principal.FindAll(ClaimTypes.Role).Select(x => x.Value).Should().ContainSingle().Which.Should().Be("Admin");
        principal.GetScopes().Should().Contain([
            "custom_scope",
            OpenIddictConstants.Scopes.OfflineAccess,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles
        ]);
    }
}
