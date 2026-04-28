using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Application.UnitTests;

public sealed class DbUpdateExceptionExtensionsTests
{
    [Fact]
    public void IsUniqueConstraintViolation_WhenSqlStateIsPostgresUniqueViolation_ReturnsTrue()
    {
        var exception = new DbUpdateException("Unique constraint violation.", new TestDbException("23505"));

        var result = exception.IsUniqueConstraintViolation();

        result.Should().BeTrue();
    }

    [Fact]
    public void IsUniqueConstraintViolation_WhenSqlStateIsDifferent_ReturnsFalse()
    {
        var exception = new DbUpdateException("Serialization failure.", new TestDbException("40001"));

        var result = exception.IsUniqueConstraintViolation();

        result.Should().BeFalse();
    }

    [Fact]
    public void IsUniqueConstraintViolation_WhenInnerExceptionIsNotDbException_ReturnsFalse()
    {
        var exception = new DbUpdateException("Update failed.", new InvalidOperationException("boom"));

        var result = exception.IsUniqueConstraintViolation();

        result.Should().BeFalse();
    }

    private sealed class TestDbException(string sqlState) : DbException("Database exception")
    {
        public override string? SqlState => sqlState;
    }
}
