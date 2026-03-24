using CSharpFunctionalExtensions;
using FlowChat.API.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.NotificationService.Api.Features.Notifications.Internal.ProcessUserEmailVerificationRequested;
using FlowChat.NotificationService.Application.Features.Notifications.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace FlowChat.NotificationService.UnitTests;

public sealed class ProcessUserEmailVerificationRequestedControllerTests
{
    [Fact]
    public async Task Process_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
            }
        };

        var result = await controller.Process(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = Guid.NewGuid(),
                Email = "john.doe@flowchat.local",
                UserName = "john.doe",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Process_WhenApiKeyMatches_SendsCommandAndReturnsAccepted()
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

        var result = await controller.Process(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = Guid.NewGuid(),
                Email = " john.doe@flowchat.local ",
                UserName = " john.doe ",
                DisplayName = " John Doe ",
                ConfirmationLink = " https://localhost/confirm ",
                SourceMessageKey = " source-key "
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(mediator.LastCommand);
        Assert.Equal("john.doe@flowchat.local", mediator.LastCommand!.Email);
        Assert.Equal("john.doe", mediator.LastCommand.UserName);
        Assert.Equal("John Doe", mediator.LastCommand.DisplayName);
        Assert.Equal("https://localhost/confirm", mediator.LastCommand.ConfirmationLink);
        Assert.Equal("source-key", mediator.LastCommand.SourceMessageKey);
    }

    [Fact]
    public async Task Process_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var controller = CreateController("expected-key", out _);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };
        httpContext.Request.Headers["X-Internal-Api-Key"] = "expected-key";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.Process(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = Guid.Empty,
                Email = "john.doe@flowchat.local",
                UserName = "john.doe",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static ProcessUserEmailVerificationRequestedController CreateController(
        string apiKey,
        out FakeMediator mediator)
    {
        mediator = new FakeMediator();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = apiKey
            })
            .Build();

        return new ProcessUserEmailVerificationRequestedController(
            mediator,
            new ApiSettingsManager(configuration));
    }

    private sealed class FakeMediator : IMediator
    {
        public UserEmailVerificationRequestedCommand? LastCommand { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastCommand = request as UserEmailVerificationRequestedCommand;

            if (typeof(TResponse) == typeof(Result<Unit, IDomainError>))
            {
                return Task.FromResult((TResponse)(object)Result.Success<Unit, IDomainError>(Unit.Value));
            }

            throw new NotSupportedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastCommand = request as UserEmailVerificationRequestedCommand;
            return Task.FromResult<object?>(Result.Success<Unit, IDomainError>(Unit.Value));
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastCommand = request as UserEmailVerificationRequestedCommand;
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            EmptyAsyncEnumerable<object?>();

        private static async IAsyncEnumerable<T> EmptyAsyncEnumerable<T>()
        {
            yield break;
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
