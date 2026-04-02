using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowChat.AuthService.Application.Features.User.Models;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace FlowChat.AuthService.UnitTests.Infrastructure.Services;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ValidUser_ReturnsTokenWithExpectedClaims()
    {
        var sut = CreateSut(CreateJwtSettings());
        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com",
            Roles = ["Admin", "User"]
        };

        var result = sut.GenerateToken(user);

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        token.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id.ToString());
        token.Claims.First(x => x.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(user.Email);
        token.Claims.First(x => x.Type == JwtRegisteredClaimNames.UniqueName).Value.Should().Be(user.UserName);
        token.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value)
            .Should().BeEquivalentTo(["Admin", "User"]);
    }

    [Fact]
    public void GenerateToken_ValidUser_UsesConfiguredExpirationWindow()
    {
        var sut = CreateSut(CreateJwtSettings(expiresMinutes: 45));
        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com"
        };

        var beforeGeneration = DateTime.UtcNow;

        var result = sut.GenerateToken(user);

        result.ExpiresAtUtc.Should().BeCloseTo(beforeGeneration.AddMinutes(45), TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", "TestIssuer", "TestAudience", "Missing configuration value: JwtSettings:Key.")]
    [InlineData(null, "TestIssuer", "TestAudience", "Missing configuration value: JwtSettings:Key.")]
    [InlineData("ThisIsASecretKeyForTestingPurposesOnly1234567890", "", "TestAudience", "Missing configuration value: JwtSettings:Issuer.")]
    [InlineData("ThisIsASecretKeyForTestingPurposesOnly1234567890", "TestIssuer", "", "Missing configuration value: JwtSettings:Audience.")]
    [InlineData("ThisIsASecretKeyForTestingPurposesOnly1234567890", "TestIssuer", null, "Missing configuration value: JwtSettings:Audience.")]
    public void GenerateToken_MissingConfiguration_ThrowsInvalidOperationException(
        string? key,
        string? issuer,
        string? audience,
        string expectedMessage)
    {
        var sut = CreateSut(CreateJwtSettings(key, issuer, audience));
        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com"
        };

        var act = () => sut.GenerateToken(user);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void GenerateToken_UserWithoutRoles_ReturnsTokenWithoutRoleClaims()
    {
        var sut = CreateSut(CreateJwtSettings());
        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com",
            Roles = []
        };

        var result = sut.GenerateToken(user);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        token.Claims.Where(x => x.Type == ClaimTypes.Role).Should().BeEmpty();
    }

    private static JwtTokenGenerator CreateSut(JwtSettings jwtSettings)
    {
        var apiSettingsManagerMock = new Mock<IApiSettingsManager>();
        apiSettingsManagerMock
            .Setup(x => x.GetJwtSettings())
            .Returns(jwtSettings);

        return new JwtTokenGenerator(apiSettingsManagerMock.Object);
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsRefreshTokenAndExpiry()
    {
        var settings = CreateJwtSettings(refreshTokenExpiresMinutes: 10080);
        var sut = CreateSut(settings);
        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com"
        };

        var beforeGeneration = DateTime.UtcNow;

        var result = sut.GenerateToken(user);

        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshTokenExpiresAtUtc.Should().BeCloseTo(
            beforeGeneration.AddMinutes(10080), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsRefreshTokenAndExpiry()
    {
        var settings = CreateJwtSettings(refreshTokenExpiresMinutes: 10080);
        var sut = CreateSut(settings);

        var beforeGeneration = DateTime.UtcNow;

        var result = sut.GenerateRefreshToken();

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().BeCloseTo(
            beforeGeneration.AddMinutes(10080), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_WithProvidedRefreshToken_UsesProvidedRefreshTokenData()
    {
        var sut = CreateSut(CreateJwtSettings());
        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com"
        };
        var refreshToken = "provided-refresh-token";
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);

        var result = sut.GenerateToken(user, refreshToken, refreshTokenExpiresAtUtc);

        result.RefreshToken.Should().Be(refreshToken);
        result.RefreshTokenExpiresAtUtc.Should().Be(refreshTokenExpiresAtUtc);
    }

    [Fact]
    public void GenerateToken_CalledTwice_ReturnsDifferentRefreshTokens()
    {
        var sut = CreateSut(CreateJwtSettings());
        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com"
        };

        var result1 = sut.GenerateToken(user);
        var result2 = sut.GenerateToken(user);

        result1.RefreshToken.Should().NotBe(result2.RefreshToken);
    }

    [Fact]
    public void ExtractUserIdFromExpiredToken_ValidExpiredToken_ReturnsUserId()
    {
        var sut = CreateSut(CreateJwtSettings(expiresMinutes: 0));
        var userId = Guid.NewGuid();
        var user = new AuthenticatedUser
        {
            Id = userId,
            UserName = "flower",
            Email = "flower@example.com"
        };

        var token = sut.GenerateToken(user);

        var extractedUserId = sut.ExtractUserIdFromExpiredToken(token.AccessToken);

        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void ExtractUserIdFromExpiredToken_ValidNonExpiredToken_ReturnsUserId()
    {
        var sut = CreateSut(CreateJwtSettings(expiresMinutes: 60));
        var userId = Guid.NewGuid();
        var user = new AuthenticatedUser
        {
            Id = userId,
            UserName = "flower",
            Email = "flower@example.com"
        };

        var token = sut.GenerateToken(user);

        var extractedUserId = sut.ExtractUserIdFromExpiredToken(token.AccessToken);

        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void ExtractUserIdFromExpiredToken_MalformedToken_ReturnsNull()
    {
        var sut = CreateSut(CreateJwtSettings());

        var result = sut.ExtractUserIdFromExpiredToken("not-a-jwt-token");

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractUserIdFromExpiredToken_TokenSignedWithDifferentKey_ReturnsNull()
    {
        var sutOriginal = CreateSut(CreateJwtSettings(key: "OriginalKeyThatIsLongEnoughForHmacSha256Algorithm!"));
        var sutDifferent = CreateSut(CreateJwtSettings(key: "DifferentKeyThatIsLongEnoughForHmacSha256Algorithm!"));

        var user = new AuthenticatedUser
        {
            Id = Guid.NewGuid(),
            UserName = "flower",
            Email = "flower@example.com"
        };

        var token = sutOriginal.GenerateToken(user);

        var result = sutDifferent.ExtractUserIdFromExpiredToken(token.AccessToken);

        result.Should().BeNull();
    }

    private static JwtSettings CreateJwtSettings(
        string? key = "ThisIsASecretKeyForTestingPurposesOnly1234567890",
        string? issuer = "TestIssuer",
        string? audience = "TestAudience",
        int expiresMinutes = 30,
        int refreshTokenExpiresMinutes = 10080)
    {
        return new JwtSettings
        {
            Key = key ?? string.Empty,
            Issuer = issuer ?? string.Empty,
            Audience = audience ?? string.Empty,
            ExpiresMinutes = expiresMinutes,
            RefreshTokenExpiresMinutes = refreshTokenExpiresMinutes
        };
    }
}
