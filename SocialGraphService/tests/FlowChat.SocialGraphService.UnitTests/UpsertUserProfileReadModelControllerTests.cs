using FlowChat.API.Abstractions;
using FlowChat.Application.Abstractions;
using FlowChat.SocialGraphService.Api.Features.UserProfiles.Internal.UpsertUserProfileReadModel;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfiles;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UpsertUserProfileReadModelControllerTests
{
    [Fact]
    public async Task Upsert_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", out _, out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
            }
        };

        var result = await controller.Upsert(
            new UpsertUserProfileReadModelRequest
            {
                UserProfileId = Guid.NewGuid(),
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Upsert_WhenApiKeyMatches_UpsertsReadModelAndSavesChanges()
    {
        var controller = CreateController("expected-key", out var repository, out var unitOfWork);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };
        httpContext.Request.Headers["X-Internal-Api-Key"] = "expected-key";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.Upsert(
            new UpsertUserProfileReadModelRequest
            {
                UserProfileId = Guid.NewGuid(),
                UserName = " jdoe ",
                DisplayName = " John Doe ",
                MainEmail = " john@example.com "
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(repository.LastUpsertedReadModel);
        Assert.Equal("jdoe", repository.LastUpsertedReadModel!.UserName);
        Assert.Equal("John Doe", repository.LastUpsertedReadModel.DisplayName);
        Assert.Equal("john@example.com", repository.LastUpsertedReadModel.MainEmail);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Upsert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var controller = CreateController("expected-key", out _, out _);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };
        httpContext.Request.Headers["X-Internal-Api-Key"] = "expected-key";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.Upsert(
            new UpsertUserProfileReadModelRequest
            {
                UserProfileId = Guid.Empty,
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static UpsertUserProfileReadModelController CreateController(
        string apiKey,
        out FakeUserProfileReadModelRepository repository,
        out FakeUnitOfWork unitOfWork)
    {
        repository = new FakeUserProfileReadModelRepository();
        unitOfWork = new FakeUnitOfWork();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = apiKey
            })
            .Build();

        return new UpsertUserProfileReadModelController(
            repository,
            unitOfWork,
            new ApiSettingsManager(configuration));
    }

    private sealed class FakeUserProfileReadModelRepository : IUserProfileReadModelRepository
    {
        public UserProfileReadModel? LastUpsertedReadModel { get; private set; }

        public Task<bool> UpsertAsync(UserProfileReadModel readModel, CancellationToken cancellationToken = default)
        {
            LastUpsertedReadModel = readModel;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            var result = await operation(cancellationToken);
            SaveChangesCallCount++;
            return result;
        }
    }

    private sealed class SingleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType.IsInstanceOfType(service) ? service : null;
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new()
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
    }
}
