using FlowChat.Core.Domain;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FluentAssertions;

namespace FlowChat.PresenceService.UnitTests;

public sealed class UserPresencePreferencesTests
{
    [Fact]
    public void Create_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        var act = () => UserPresencePreferences.Create(Guid.Empty, PresenceStatus.Active);

        act.Should().Throw<ArgumentException>()
            .WithMessage("UserId is required.*");
    }

    [Theory]
    [InlineData(PresenceStatus.Active)]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public void Create_WithExplicitStatus_SetsPreferredStatus(PresenceStatus status)
    {
        var userId = Guid.NewGuid();

        var preferences = UserPresencePreferences.Create(userId, status);

        preferences.UserId.Should().Be(userId);
        preferences.PreferredStatus.Should().Be(status);
    }

    [Fact]
    public void Create_WithAfkStatus_ThrowsArgumentException()
    {
        var act = () => UserPresencePreferences.Create(Guid.NewGuid(), PresenceStatus.AFK);

        act.Should().Throw<ArgumentException>()
            .WithMessage("AFK cannot be saved as a default startup status.*");
    }

    [Theory]
    [InlineData(PresenceStatus.Active)]
    [InlineData(PresenceStatus.Busy)]
    [InlineData(PresenceStatus.Invisible)]
    public void SetPreferredStatus_WithExplicitStatus_UpdatesPreferredStatus(PresenceStatus status)
    {
        var preferences = UserPresencePreferences.Create(Guid.NewGuid(), PresenceStatus.Busy);

        preferences.SetPreferredStatus(status);

        preferences.PreferredStatus.Should().Be(status);
    }

    [Fact]
    public void SetPreferredStatus_WithAfkStatus_ThrowsArgumentException()
    {
        var preferences = UserPresencePreferences.Create(Guid.NewGuid(), PresenceStatus.Busy);

        var act = () => preferences.SetPreferredStatus(PresenceStatus.AFK);

        act.Should().Throw<ArgumentException>()
            .WithMessage("AFK cannot be saved as a default startup status.*");
    }
}
