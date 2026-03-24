using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;

namespace FlowChat.Domain.Abstractions.ValueObjects;

public sealed class EmailAddress : IEquatable<EmailAddress>
{
    public const string InvalidEmailAddressMessage =
        "Email address must be a valid email address.";

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value.ToLowerInvariant();
    }

    public static EmailAddress Create(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var trimmedAddress = address.Trim();
        if (!MailAddress.TryCreate(trimmedAddress, out var parsedAddress) ||
            !string.Equals(parsedAddress.Address, trimmedAddress, StringComparison.OrdinalIgnoreCase))
        {
            throw CreateValidationException();
        }

        return new EmailAddress(trimmedAddress);
    }

    public static bool TryCreate(
        string? address,
        [NotNullWhen(true)] out EmailAddress? emailAddress)
    {
        emailAddress = null;
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        try
        {
            emailAddress = Create(address);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool Equals(EmailAddress? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is EmailAddress other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(EmailAddress? left, EmailAddress? right) => Equals(left, right);

    public static bool operator !=(EmailAddress? left, EmailAddress? right) => !Equals(left, right);

    public override string ToString() => Value;

    private static ArgumentException CreateValidationException(Exception? innerException = null)
    {
        return new ArgumentException(
            InvalidEmailAddressMessage,
            "address",
            innerException);
    }
}
