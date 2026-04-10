using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace FlowChat.Shared.Domain.ValueObjects;

public sealed class FriendlyUserId : IEquatable<FriendlyUserId>
{
    public const int MaxLength = 100;
    public const string InvalidFriendlyUserIdMessage =
        "Friendly user id must contain only lowercase English letters, digits, '-', or '.', and must start and end with a letter or digit.";

    private static readonly Regex ValidationRegex = new(
        "^[a-z0-9](?:[a-z0-9.-]*[a-z0-9])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private FriendlyUserId(string value)
    {
        Value = value;
    }

    public static FriendlyUserId Create(string friendlyUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyUserId);

        var normalizedFriendlyUserId = friendlyUserId.Trim().ToLowerInvariant();
        if (normalizedFriendlyUserId.Length > MaxLength || !ValidationRegex.IsMatch(normalizedFriendlyUserId))
        {
            throw CreateValidationException();
        }

        return new FriendlyUserId(normalizedFriendlyUserId);
    }

    public static bool TryCreate(
        string? friendlyUserId,
        [NotNullWhen(true)] out FriendlyUserId? normalizedFriendlyUserId)
    {
        normalizedFriendlyUserId = null;
        if (string.IsNullOrWhiteSpace(friendlyUserId))
        {
            return false;
        }

        try
        {
            normalizedFriendlyUserId = Create(friendlyUserId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool Equals(FriendlyUserId? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is FriendlyUserId other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(FriendlyUserId? left, FriendlyUserId? right) => Equals(left, right);

    public static bool operator !=(FriendlyUserId? left, FriendlyUserId? right) => !Equals(left, right);

    public override string ToString() => Value;

    private static ArgumentException CreateValidationException()
    {
        return new ArgumentException(
            InvalidFriendlyUserIdMessage,
            "friendlyUserId");
    }
}
