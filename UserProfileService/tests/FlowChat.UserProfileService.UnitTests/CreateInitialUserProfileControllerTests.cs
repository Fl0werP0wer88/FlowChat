using CSharpFunctionalExtensions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Api.Features.UserProfiles.Internal.CreateInitialUserProfile;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class CreateInitialUserProfileControllerTests
{
    [Fact]
    public async Task CreateInitialUserProfile_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
            }
        };

        var result = await controller.CreateInitialUserProfile(
            new CreateInitialUserProfileRequest
            {
                UserId = Guid.NewGuid(),
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateInitialUserProfile_WhenApiKeyMatches_DispatchesCommand()
    {
        var controller = CreateController("expected-key", out var mediator);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };
        httpContext.Request.Headers["X-Internal-Api-Key"] = "expected-key";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.CreateInitialUserProfile(
            new CreateInitialUserProfileRequest
            {
                UserId = Guid.NewGuid(),
                UserName = "jdoe",
                DisplayName = "John Doe",
                Email = "john@example.com"
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        var command = Assert.IsType<CreateInitialUserProfileCommand>(mediator.LastSentRequest);
        Assert.Equal("jdoe", command.UserName);
        Assert.Equal("John Doe", command.DisplayName);
        Assert.Equal("john@example.com", command.Email);
    }

    private static CreateInitialUserProfileController CreateController(string apiKey, out CapturingMediator mediator)
    {
        mediator = new CapturingMediator();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = apiKey
            })
            .Build();

        return new CreateInitialUserProfileController(mediator, new ApiSettingsManager(configuration));
    }

    private sealed class CapturingMediator : IMediator
    {
        public object? LastSentRequest { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastSentRequest = request;
            return Task.FromResult((TResponse)(object)Result.Success<Guid, IDomainError>(Guid.NewGuid()));
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastSentRequest = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastSentRequest = request;
            return Task.FromResult<object?>(Result.Success<Guid, IDomainError>(Guid.NewGuid()));
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<object?>();
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
