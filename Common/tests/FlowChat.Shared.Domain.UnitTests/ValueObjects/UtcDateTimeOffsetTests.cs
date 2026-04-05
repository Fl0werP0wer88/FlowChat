using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace FlowChat.Shared.Domain.UnitTests.ValueObjects;

public sealed class UtcDateTimeOffsetTests
{
    [Fact]
    public void Create_WithUtcValue_CreatesValueObject()
    {
        var value = new DateTimeOffset(2026, 4, 5, 12, 30, 45, TimeSpan.Zero);

        var utcDateTimeOffset = UtcDateTimeOffset.Create(value);

        utcDateTimeOffset.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithNonUtcValue_ThrowsArgumentException()
    {
        var value = new DateTimeOffset(2026, 4, 5, 14, 30, 45, TimeSpan.FromHours(2));

        var act = () => UtcDateTimeOffset.Create(value);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage($"{UtcDateTimeOffset.InvalidUtcDateTimeOffsetMessage}*");
    }

    [Fact]
    public void TryCreate_WithUtcValue_ReturnsTrue()
    {
        var value = new DateTimeOffset(2026, 4, 5, 12, 30, 45, TimeSpan.Zero);

        var result = UtcDateTimeOffset.TryCreate(value, out var utcDateTimeOffset);

        result.Should().BeTrue();
        utcDateTimeOffset.Should().NotBeNull();
        utcDateTimeOffset!.Value.Should().Be(value);
    }

    [Fact]
    public void TryCreate_WithNullValue_ReturnsFalse()
    {
        var result = UtcDateTimeOffset.TryCreate(null, out var utcDateTimeOffset);

        result.Should().BeFalse();
        utcDateTimeOffset.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithNonUtcValue_ReturnsFalse()
    {
        var value = new DateTimeOffset(2026, 4, 5, 14, 30, 45, TimeSpan.FromHours(2));

        var result = UtcDateTimeOffset.TryCreate(value, out var utcDateTimeOffset);

        result.Should().BeFalse();
        utcDateTimeOffset.Should().BeNull();
    }

    [Fact]
    public void Equality_WithSameUtcValue_ReturnsTrue()
    {
        var value = new DateTimeOffset(2026, 4, 5, 12, 30, 45, TimeSpan.Zero);

        var first = UtcDateTimeOffset.Create(value);
        var second = UtcDateTimeOffset.Create(value);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
    }

    [Fact]
    public void ToString_WithUtcValue_ReturnsRoundTripRepresentation()
    {
        var value = new DateTimeOffset(2026, 4, 5, 12, 30, 45, TimeSpan.Zero);

        var utcDateTimeOffset = UtcDateTimeOffset.Create(value);

        utcDateTimeOffset.ToString().Should().Be("2026-04-05T12:30:45.0000000+00:00");
    }
}
