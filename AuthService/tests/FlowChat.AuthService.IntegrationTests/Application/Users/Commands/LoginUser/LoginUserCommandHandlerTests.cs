using System.Diagnostics.CodeAnalysis;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Commands.LoginUser;
using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.Identity;
using FlowChat.AuthService.Persistence.Repositories;
using FlowChat.AuthService.Persistence.UnitOfWork;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback;
using Silverback.Storage;

namespace FlowChat.AuthService.IntegrationTests.Application.Users.Commands.LoginUser;

public sealed class LoginUserCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly IdentityRepository _identityRepository;
    private readonly AppDbContextUnitOfWork _unitOfWork;
    private readonly LoginUserCommandHandler _sut;

    public LoginUserCommandHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

        services.AddIdentityCore<UserEntity>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
            })
            .AddRoles<RoleEntity>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserStore<UserEntity>>(sp =>
        {
            var context = sp.GetRequiredService<AppDbContext>();
            var describer = sp.GetRequiredService<IdentityErrorDescriber>();

            return new UserStore<UserEntity, RoleEntity, AppDbContext, Guid>(
                context,
                describer)
            {
                AutoSaveChanges = false
            };
        });

        services.AddDataProtection();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();

        var userManager = _serviceProvider.GetRequiredService<UserManager<UserEntity>>();
        _identityRepository = new IdentityRepository(userManager);

        var jwtTokenGenerator = new JwtTokenGenerator(new ApiSettingsManager(CreateConfiguration()));
        _unitOfWork = new AppDbContextUnitOfWork(
            _serviceProvider.GetRequiredService<AppDbContext>(),
            new TestSilverbackContext());

        _sut = new LoginUserCommandHandler(
            _identityRepository,
            jwtTokenGenerator,
            new NoOpDomainEventDispatcher(),
            _unitOfWork);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Handle_ValidCredentialsWithManualUnitOfWork_SavesRefreshTokenWithoutConcurrencyConflict()
    {
        var identity = Identity.Create(Guid.NewGuid(), "login-user", "login-user@test.com");
        await ExecuteInUnitOfWorkAsync(token => _identityRepository.CreateUserAsync(identity, "Pass1234!", token));
        await ConfirmEmailAsync(identity);

        var result = await _sut.Handle(
            new LoginUserCommand
            {
                Login = "login-user@test.com",
                Password = "Pass1234!"
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var storedRefreshToken = await _identityRepository.GetRefreshTokenAsync(identity.Id.Value, CancellationToken.None);

        storedRefreshToken.Should().NotBeNull();
        storedRefreshToken!.Value.Token.Should().Be(result.Value.RefreshToken);
        storedRefreshToken.Value.ExpiresAtUtc.Should().Be(result.Value.RefreshTokenExpiresAtUtc);
    }

    private async Task ConfirmEmailAsync(Identity identity)
    {
        var confirmedIdentity = Identity.Restore(
            identity.Id.Value,
            identity.UserName,
            identity.Email,
            identity.PhoneNumber,
            emailConfirmed: true,
            phoneNumberConfirmed: identity.PhoneNumberConfirmed,
            firstName: identity.FirstName,
            lastName: identity.LastName);

        await ExecuteInUnitOfWorkAsync(token => _identityRepository.UpdateAsync(confirmedIdentity, token));
    }

    private async Task ExecuteInUnitOfWorkAsync(Func<CancellationToken, Task> operation)
    {
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await operation(token);
                return true;
            },
            CancellationToken.None);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("JwtSettings:Key", "ThisIsASecretKeyForTestingPurposesOnly1234567890"),
                new KeyValuePair<string, string?>("JwtSettings:Issuer", "FlowChatTests"),
                new KeyValuePair<string, string?>("JwtSettings:Audience", "FlowChatTests"),
                new KeyValuePair<string, string?>("JwtSettings:ExpiresMinutes", "60"),
                new KeyValuePair<string, string?>("JwtSettings:RefreshTokenExpiresMinutes", "10080")
            ])
            .Build();
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestSilverbackContext : ISilverbackContext
    {
        private readonly Dictionary<Guid, object> _objects = [];

        public IServiceProvider ServiceProvider => EmptyServiceProvider.Instance;

        public void AddObject(Guid objectTypeId, object obj)
        {
            if (_objects.TryGetValue(objectTypeId, out var existing) && !ReferenceEquals(existing, obj))
            {
                throw new InvalidOperationException($"An object of type {objectTypeId} has already been added.");
            }

            _objects[objectTypeId] = obj;
        }

        public void SetObject(Guid objectTypeId, object obj) => _objects[objectTypeId] = obj;

        public bool RemoveObject(Guid objectTypeId) => _objects.Remove(objectTypeId);

        public T GetObject<T>(Guid objectTypeId) => (T)GetObject(objectTypeId);

        public object GetObject(Guid objectTypeId)
        {
            if (!_objects.TryGetValue(objectTypeId, out var value))
            {
                throw new InvalidOperationException($"The object with type id {objectTypeId} was not found.");
            }

            return value;
        }

        public bool TryGetObject<T>(Guid objectTypeId, [NotNullWhen(true)] out T? obj)
        {
            if (TryGetObject(objectTypeId, out object? value))
            {
                if (value is not T typedValue)
                {
                    throw new InvalidOperationException($"The object with type id {objectTypeId} is not of type {typeof(T)}.");
                }

                obj = typedValue;
                return true;
            }

            obj = default;
            return false;
        }

        public bool TryGetObject(Guid objectTypeId, [NotNullWhen(true)] out object? obj) => _objects.TryGetValue(objectTypeId, out obj);

        public T GetOrAddObject<T>(Guid objectTypeId, Func<T> factory)
        {
            if (TryGetObject(objectTypeId, out T? existing))
            {
                return existing!;
            }

            var created = factory() ?? throw new InvalidOperationException("The factory returned null.");
            AddObject(objectTypeId, created);

            return created;
        }

        public TObject GetOrAddObject<TObject, TArg>(Guid objectTypeId, Func<TArg, TObject> factory, TArg argument)
        {
            if (TryGetObject(objectTypeId, out TObject? existing))
            {
                return existing!;
            }

            var created = factory(argument) ?? throw new InvalidOperationException("The factory returned null.");
            AddObject(objectTypeId, created);

            return created;
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
