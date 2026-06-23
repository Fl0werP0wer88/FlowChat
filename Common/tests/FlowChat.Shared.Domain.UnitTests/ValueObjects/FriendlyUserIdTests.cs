using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace FlowChat.Shared.Domain.UnitTests.ValueObjects;

public sealed class FriendlyUserIdTests
{
    [Theory]
    [InlineData("john")]
    [InlineData("john-doe")]
    [InlineData("john.doe")]
    [InlineData("j0hn.d0e-1")]
    public void Create_WithValidValue_CreatesValueObject(string value)
    {
        var friendlyUserId = FriendlyUserId.Create(value);

        friendlyUserId.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithTrimmedAndUppercaseValue_NormalizesToLowercase()
    {
        var friendlyUserId = FriendlyUserId.Create("  John.Doe-1  ");

        friendlyUserId.Value.Should().Be("john.doe-1");
    }

    [Theory]
    [InlineData("-john")]
    [InlineData("john-")]
    [InlineData(".john")]
    [InlineData("john.")]
    [InlineData("john_doe")]
    [InlineData("john doe")]
    public void Create_WithInvalidValue_ThrowsArgumentException(string value)
    {
        var act = () => FriendlyUserId.Create(value);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage($"{FriendlyUserId.InvalidFriendlyUserIdMessage}*");
    }

    [Fact]
    public void Create_WhenValueExceedsMaxLength_ThrowsArgumentException()
    {
        var tooLongValue = $"a{new string('b', FriendlyUserId.MaxLength - 1)}c";

        var act = () => FriendlyUserId.Create(tooLongValue);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage($"{FriendlyUserId.InvalidFriendlyUserIdMessage}*");
    }

    [Fact]
    public void TryCreate_WithValidValue_ReturnsTrue()
    {
        var result = FriendlyUserId.TryCreate("  John  ", out var friendlyUserId);

        result.Should().BeTrue();
        friendlyUserId.Should().NotBeNull();
        friendlyUserId!.Value.Should().Be("john");
    }

    [Fact]
    public void TryCreate_WithInvalidValue_ReturnsFalse()
    {
        var result = FriendlyUserId.TryCreate("john_doe", out var friendlyUserId);

        result.Should().BeFalse();
        friendlyUserId.Should().BeNull();
    }

    [Fact]
    public void Equality_WithSameNormalizedValue_ReturnsTrue()
    {
        var first = FriendlyUserId.Create("John");
        var second = FriendlyUserId.Create("john");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
    }
}
