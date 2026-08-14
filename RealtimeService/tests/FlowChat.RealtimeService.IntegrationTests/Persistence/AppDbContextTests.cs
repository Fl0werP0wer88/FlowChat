using FlowChat.RealtimeService.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowChat.RealtimeService.IntegrationTests.Persistence;

public sealed class AppDbContextTests
{
    [Fact]
    public void Constructor_WithDbConnection_UsesProvidedConnection()
    {
        using var connection = new NpgsqlConnection();
        using var dbContext = new AppDbContext(connection);

        dbContext.Database.GetDbConnection().Should().BeSameAs(connection);
    }
}
