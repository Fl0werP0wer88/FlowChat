using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace FlowChat.Shared.Domain.ValueObjects;

public sealed class UtcDateTimeOffset : IEquatable<UtcDateTimeOffset>, IComparable<UtcDateTimeOffset>
{
    public const string InvalidUtcDateTimeOffsetMessage =
        "DateTimeOffset value must be in UTC.";

    public static UtcDateTimeOffset UtcNow => Create(DateTimeOffset.UtcNow);

    public DateTimeOffset Value { get; }
    public TimeSpan Offset => Value.Offset;
    public DateTime UtcDateTime => Value.UtcDateTime;

    private UtcDateTimeOffset(DateTimeOffset value)
    {
        Value = value;
    }

    public static UtcDateTimeOffset Create(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw CreateValidationException();
        }

        return new UtcDateTimeOffset(value);
    }

    public static bool TryCreate(
        DateTimeOffset? value,
        [NotNullWhen(true)] out UtcDateTimeOffset? utcDateTimeOffset)
    {
        utcDateTimeOffset = null;
        if (value is null)
        {
            return false;
        }

        try
        {
            utcDateTimeOffset = Create(value.Value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool Equals(UtcDateTimeOffset? other)
    {
        if (other is null)
        {
            return false;
        }

        return Value.Equals(other.Value);
    }

    public UtcDateTimeOffset Add(TimeSpan timeSpan) => Create(Value.Add(timeSpan));

    public UtcDateTimeOffset AddDays(double days) => Create(Value.AddDays(days));

    public UtcDateTimeOffset AddHours(double hours) => Create(Value.AddHours(hours));

    public UtcDateTimeOffset AddMinutes(double minutes) => Create(Value.AddMinutes(minutes));

    public int CompareTo(UtcDateTimeOffset? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Value.CompareTo(other.Value);
    }

    public override bool Equals(object? obj) => obj is UtcDateTimeOffset other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(UtcDateTimeOffset? left, UtcDateTimeOffset? right) => Equals(left, right);

    public static bool operator !=(UtcDateTimeOffset? left, UtcDateTimeOffset? right) => !Equals(left, right);

    public static bool operator <(UtcDateTimeOffset left, UtcDateTimeOffset right) => left.CompareTo(right) < 0;

    public static bool operator <=(UtcDateTimeOffset left, UtcDateTimeOffset right) => left.CompareTo(right) <= 0;

    public static bool operator >(UtcDateTimeOffset left, UtcDateTimeOffset right) => left.CompareTo(right) > 0;

    public static bool operator >=(UtcDateTimeOffset left, UtcDateTimeOffset right) => left.CompareTo(right) >= 0;

    public static implicit operator UtcDateTimeOffset(DateTimeOffset value) => Create(value);

    public static implicit operator DateTimeOffset(UtcDateTimeOffset value) => value.Value;

    public override string ToString() => Value.ToString("O", CultureInfo.InvariantCulture);

    private static ArgumentException CreateValidationException()
    {
        return new ArgumentException(
            InvalidUtcDateTimeOffsetMessage,
            "value");
    }
}
