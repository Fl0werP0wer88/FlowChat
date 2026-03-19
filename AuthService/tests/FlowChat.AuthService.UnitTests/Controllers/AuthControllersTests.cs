using CSharpFunctionalExtensions;
using AutoMapper;
using FlowChat.AuthService.API.Features.Users.ConfirmUserEmail;
using FlowChat.AuthService.API.Features.Users.LoginUser;
using FlowChat.AuthService.API.Features.Users.RegisterUser;
using FlowChat.AuthService.Application.Users.Commands.ConfirmUserEmail;
using FlowChat.AuthService.Application.Users.Commands.LoginUser;
using FlowChat.AuthService.Application.Users.Commands.RegisterUser;
using FlowChat.Domain.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowChat.AuthService.UnitTests.Controllers;

public sealed class AuthControllersTests
{
    [Fact]
    public async Task Register_ReturnsConflictProblemDetails_WhenRegistrationFailsWithConflict()
    {
        var mediator = new TestMediator(request =>
            request switch
            {
                RegisterUserCommand => Result.Failure<RegisterUserCommandResponse, IDomainError>(
                    DomainError.Conflict("User with the provided username or email already exists.")),
                _ => throw new InvalidOperationException("Unexpected request.")
            });
        var controller = CreateController(new RegisterUserController(mediator, CreateMapper()));

        var result = await controller.Create(
            new RegisterUserRequest
            {
                UserName = "jdoe",
                Email = "john@example.com",
                PhoneNumber = null,
                Password = "Password123!"
            },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("User with the provided username or email already exists.", problemDetails.Detail);
    }

    [Fact]
    public async Task ConfirmEmail_ReturnsNotFoundProblemDetails_WhenUserDoesNotExist()
    {
        var mediator = new TestMediator(request =>
            request switch
            {
                ConfirmUserEmailCommand => Result.Failure<ConfirmUserEmailCommandResponse, IDomainError>(
                    DomainError.NotFound("User was not found.")),
                _ => throw new InvalidOperationException("Unexpected request.")
            });
        var controller = CreateController(new ConfirmUserEmailController(mediator, CreateMapper()));

        var result = await controller.ConfirmEmail(
            new ConfirmUserEmailRequest
            {
                UserId = Guid.NewGuid(),
                Token = "token"
            },
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("User was not found.", problemDetails.Detail);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorizedProblemDetails_WhenCredentialsAreInvalid()
    {
        var mediator = new TestMediator(request =>
            request switch
            {
                LoginUserCommand => Result.Failure<LoginUserCommandResponse, IDomainError>(
                    DomainError.Unauthorized("Invalid credentials or account is not confirmed.")),
                _ => throw new InvalidOperationException("Unexpected request.")
            });
        var controller = CreateController(new LoginUserController(mediator, CreateMapper()));

        var result = await controller.Login(
            new LoginUserRequest
            {
                Login = "jdoe",
                Password = "Password123!"
            },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorized.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
        Assert.Equal("Invalid credentials or account is not confirmed.", problemDetails.Detail);
    }

    private static TController CreateController<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
            }
        };

        return controller;
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(RegisterUserController).Assembly));

        return configuration.CreateMapper();
    }

    private sealed class TestMediator(Func<object, object?> handler) : IMediator
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult((TResponse)handler(request)!);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult(handler(request));

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
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
            string? instance = null)
        {
            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            return new ValidationProblemDetails(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
        }
    }
}
