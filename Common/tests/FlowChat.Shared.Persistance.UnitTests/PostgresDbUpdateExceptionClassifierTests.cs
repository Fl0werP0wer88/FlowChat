using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.Shared.Persistance.UnitTests;

public sealed class PostgresDbUpdateExceptionClassifierTests
{
    [Fact]
    public void IsExpectedUniqueConstraintViolation_WhenUniqueViolationHasExpectedConstraint_ReturnsTrue()
    {
        var classifier = new PostgresDbUpdateExceptionClassifier();
        var exception = CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, "ux_messages_id");

        var result = classifier.IsExpectedUniqueConstraintViolation(exception, ["ux_messages_id"]);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpectedUniqueConstraintViolation_WhenUniqueViolationHasDifferentConstraint_ReturnsFalse()
    {
        var classifier = new PostgresDbUpdateExceptionClassifier();
        var exception = CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, "ux_messages_email");

        var result = classifier.IsExpectedUniqueConstraintViolation(exception, ["ux_messages_id"]);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpectedUniqueConstraintViolation_WhenUniqueViolationHasNoConstraintFilter_ReturnsTrue()
    {
        var classifier = new PostgresDbUpdateExceptionClassifier();
        var exception = CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, "ux_messages_id");

        var result = classifier.IsExpectedUniqueConstraintViolation(exception, []);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpectedUniqueConstraintViolation_WhenSqlStateIsDifferent_ReturnsFalse()
    {
        var classifier = new PostgresDbUpdateExceptionClassifier();
        var exception = CreateDbUpdateException(PostgresErrorCodes.SerializationFailure, "ux_messages_id");

        var result = classifier.IsExpectedUniqueConstraintViolation(exception, ["ux_messages_id"]);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpectedUniqueConstraintViolation_WhenInnerExceptionIsNotPostgresException_ReturnsFalse()
    {
        var classifier = new PostgresDbUpdateExceptionClassifier();
        var exception = new DbUpdateException("Update failed.", new InvalidOperationException("boom"));

        var result = classifier.IsExpectedUniqueConstraintViolation(exception, []);

        result.Should().BeFalse();
    }

    private static DbUpdateException CreateDbUpdateException(string sqlState, string constraintName)
    {
        return new DbUpdateException(
            "Update failed.",
            new PostgresException(
                messageText: "Database exception",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: sqlState,
                constraintName: constraintName));
    }
}
