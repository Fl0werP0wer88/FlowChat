using System.Diagnostics.CodeAnalysis;
using PhoneNumbers;

namespace FlowChat.UserProfileService.Domain.ValueObjects;

public sealed record PhoneNumber
{
    public const string InvalidPhoneNumberMessage =
        "Phone number must be a valid international phone number in E.164 format.";

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        var trimmedNumber = number.Trim();
        if (!trimmedNumber.StartsWith('+'))
        {
            throw CreateValidationException();
        }

        try
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            var parsedNumber = phoneNumberUtil.Parse(trimmedNumber, null);

            if (!phoneNumberUtil.IsPossibleNumber(parsedNumber))
            {
                throw CreateValidationException();
            }

            var normalizedNumber = phoneNumberUtil.Format(parsedNumber, PhoneNumberFormat.E164);
            return new PhoneNumber(normalizedNumber);
        }
        catch (NumberParseException exception)
        {
            throw CreateValidationException(exception);
        }
    }

    public static bool TryCreate(
        string? number,
        [NotNullWhen(true)] out PhoneNumber? phoneNumber)
    {
        phoneNumber = null;
        if (string.IsNullOrWhiteSpace(number))
        {
            return false;
        }

        try
        {
            phoneNumber = Create(number);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public override string ToString() => Value;

    private static ArgumentException CreateValidationException(Exception? innerException = null)
    {
        return new ArgumentException(
            InvalidPhoneNumberMessage,
            "number",
            innerException);
    }
}
