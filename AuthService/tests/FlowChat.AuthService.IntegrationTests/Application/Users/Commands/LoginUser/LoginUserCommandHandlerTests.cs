using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Features.User.Commands.LoginUser;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.Repositories;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Silverback;

namespace FlowChat.AuthService.IntegrationTests.Application.Users.Commands.LoginUser;

public sealed class LoginUserCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly AccountRepository _accountRepository;
    private readonly PasswordHashingService _passwordHashingService = new();
    private readonly LoginUserCommandHandler _sut;

    public LoginUserCommandHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseOpenIddict()
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _accountRepository = new AccountRepository(_dbContext);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Audience"] = "FlowChat.Client"
            })
            .Build();

        var tokenService = new OpenIddictTokenService(new AppSettingsProvider(configuration));
        var dispatcherMock = new Mock<IDomainEventDispatcher>();
        dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<LoginUserCommandResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<LoginUserCommandResponse>>>, CancellationToken>((operation, ct) => operation(ct));

        _sut = new LoginUserCommandHandler(
            _accountRepository,
            _passwordHashingService,
            tokenService,
            dispatcherMock.Object,
            unitOfWorkMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ReturnsPrincipalBuiltFromPersistedAccount()
    {
        var account = FlowChat.AuthService.Domain.Entities.Account.Account.Create(
            Id<FlowChat.AuthService.Domain.Entities.Account.Account>.New(),
            "flower",
            EmailAddress.Create("flower@example.com"),
            _passwordHashingService.HashPassword("P@ssw0rd!"),
            _passwordHashingService.GenerateSecurityStamp());
        account.ConfirmEmail();

        await _accountRepository.CreateAsync(account, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.Handle(
            new LoginUserCommand
            {
                Login = "flower@example.com",
                Password = "P@ssw0rd!",
                Scopes = ["offline_access"]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Grant.Principal.FindFirst(OpenIddict.Abstractions.OpenIddictConstants.Claims.Email)!.Value.Should().Be("flower@example.com");
        result.Value.Grant.Principal.FindFirst(OpenIddict.Abstractions.OpenIddictConstants.Claims.PreferredUsername)!.Value.Should().Be("flower");
    }
}
