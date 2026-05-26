using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.Shared.Persistance.UnitTests;

public sealed class PostgresDbUpdateExceptionClassifierTests
{
    [Fact]
    public void IsExpectedIdempotencyConflict_WhenUniqueViolationHasExpectedConstraintForKey_ReturnsTrue()
    {
        var classifier = CreateClassifier("chat.send-message", ["ux_messages_id"]);
        var exception = CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, "ux_messages_id");

        var result = classifier.IsIdempotencyConflict(exception, "chat.send-message");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpectedIdempotencyConflict_WhenUniqueViolationHasDifferentConstraintForKey_ReturnsFalse()
    {
        var classifier = CreateClassifier("chat.send-message", ["ux_messages_id"]);
        var exception = CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, "ux_messages_email");

        var result = classifier.IsIdempotencyConflict(exception, "chat.send-message");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpectedIdempotencyConflict_WhenIdempotencyKeyIsNotConfigured_ReturnsFalse()
    {
        var classifier = CreateClassifier("chat.send-message", ["ux_messages_id"]);
        var exception = CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, "ux_messages_id");

        var result = classifier.IsIdempotencyConflict(exception, "chat.create-room");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpectedIdempotencyConflict_WhenSqlStateIsDifferent_ReturnsFalse()
    {
        var classifier = CreateClassifier("chat.send-message", ["ux_messages_id"]);
        var exception = CreateDbUpdateException(PostgresErrorCodes.SerializationFailure, "ux_messages_id");

        var result = classifier.IsIdempotencyConflict(exception, "chat.send-message");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpectedIdempotencyConflict_WhenInnerExceptionIsNotPostgresException_ReturnsFalse()
    {
        var classifier = CreateClassifier("chat.send-message", ["ux_messages_id"]);
        var exception = new DbUpdateException("Update failed.", new InvalidOperationException("boom"));

        var result = classifier.IsIdempotencyConflict(exception, "chat.send-message");

        result.Should().BeFalse();
    }

    private static PostgresDbUpdateExceptionClassifier CreateClassifier(
        string idempotencyConflictKey,
        IReadOnlyCollection<string> constraintNames)
    {
        var options = new PostgresDbUpdateExceptionClassifierOptions();
        options.UniqueConstraintNamesByIdempotencyConflictKey[idempotencyConflictKey] = constraintNames;

        return new PostgresDbUpdateExceptionClassifier(options);
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
