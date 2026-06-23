using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FlowChat.SocialGraphService.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationSucceeds_CommitsChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<AppDbContext>(context);

        var contactId = await unitOfWork.ExecuteInTransactionAsync(
            async cancellationToken =>
            {
                var contact = Contact.Create(Id<Contact>.New(), Guid.NewGuid(), Guid.NewGuid(), "John Doe");
                await context.Contacts.AddAsync(contact, cancellationToken);
                return contact.Id.Value;
            },
            CancellationToken.None);

        var persistedContact = (await context.Contacts.ToListAsync()).Single(contact => contact.Id.Value == contactId);

        persistedContact.DisplayName.Should().Be("John Doe");
        persistedContact.CreatedBy.Should().Be("system");
        persistedContact.LastModifiedBy.Should().Be("system");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBackTransaction()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<AppDbContext>(context);

        var act = () => unitOfWork.ExecuteInTransactionAsync<int>(
            async cancellationToken =>
            {
                await context.Contacts.AddAsync(
                    Contact.Create(Id<Contact>.New(), Guid.NewGuid(), Guid.NewGuid(), "John Doe"),
                    cancellationToken);

                throw new InvalidOperationException("boom");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        (await context.Contacts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityWasAdded_PersistsChanges()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<AppDbContext>(context);

        context.Contacts.Add(Contact.Create(Id<Contact>.New(), Guid.NewGuid(), Guid.NewGuid(), "John Doe"));

        var affectedRows = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        affectedRows.Should().BeGreaterThan(0);
        (await context.Contacts.CountAsync()).Should().Be(1);
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
