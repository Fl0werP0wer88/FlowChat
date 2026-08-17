using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.ChatService.UnitTests.Persistence.Repositories;

public sealed class ConversationDbUpdateExceptionMapperTests
{
    private const string DuetPairConstraint = "UX_ConversationsV2_DuetParticipantPair";
    private readonly ConversationDbUpdateExceptionMapper _mapper = new();

    [Fact]
    public void Map_DuetParticipantPairUniqueViolation_ReturnsConflict()
    {
        var exception = CreateUniqueViolation(DuetPairConstraint);

        var result = _mapper.Map(exception);

        result.Should().NotBeNull();
        result!.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Be("Duet conversation already exists.");
    }

    [Fact]
    public void Map_OtherUniqueViolation_ReturnsNull()
    {
        var exception = CreateUniqueViolation("UX_OtherConstraint");

        var result = _mapper.Map(exception);

        result.Should().BeNull();
    }

    [Fact]
    public void Map_NonPostgresDbUpdateException_ReturnsNull()
    {
        var result = _mapper.Map(new DbUpdateException("Database update failed."));

        result.Should().BeNull();
    }

    private static DbUpdateException CreateUniqueViolation(string constraintName)
    {
        var postgresException = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            constraintName: constraintName);

        return new DbUpdateException("Database update failed.", postgresException);
    }
}
