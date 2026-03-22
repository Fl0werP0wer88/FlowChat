using CSharpFunctionalExtensions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Api.Features.UserProfiles.Public.AddEmail;
using FlowChat.UserProfileService.Api.Features.UserProfiles.Public.AddPhone;
using FlowChat.UserProfileService.Api.Features.UserProfiles.Public.GetUserProfile;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.AddEmail;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.AddPhone;
using FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfilesControllerTests
{
    [Fact]
    public async Task AddEmail_ReturnsOk_WithNewEmailId()
    {
        var emailId = Guid.NewGuid();
        var mediator = new TestMediator(request =>
            request switch
            {
                AddEmailCommand => Result.Success<Guid, IDomainError>(emailId),
                _ => throw new InvalidOperationException("Unexpected request.")
            });
        var controller = CreateController(new AddEmailController(mediator));

        var result = await controller.AddEmail(Guid.NewGuid(), new AddEmailRequest("john@example.com"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AddEmailResponse>(ok.Value);
        Assert.Equal(emailId, response.EmailId);
    }

    [Fact]
    public async Task AddPhone_ReturnsOk_WithNewPhoneId()
    {
        var phoneId = Guid.NewGuid();
        var mediator = new TestMediator(request =>
            request switch
            {
                AddPhoneCommand => Result.Success<Guid, IDomainError>(phoneId),
                _ => throw new InvalidOperationException("Unexpected request.")
            });
        var controller = CreateController(new AddPhoneController(mediator));

        var result = await controller.AddPhone(Guid.NewGuid(), new AddPhoneRequest("+48123123123"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AddPhoneResponse>(ok.Value);
        Assert.Equal(phoneId, response.PhoneId);
    }

    [Fact]
    public async Task GetById_ReturnsProblemDetails_WhenProfileIsMissing()
    {
        var userId = Guid.NewGuid();
        var mediator = new TestMediator(request =>
            request switch
            {
                GetUserProfileQuery => Result.Failure<UserProfileDto, IDomainError>(
                    DomainError.NotFound($"User profile '{userId}' was not found.")),
                _ => throw new InvalidOperationException("Unexpected request.")
            });
        var controller = CreateController(new UserProfilesController(mediator));

        var result = await controller.GetById(userId, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal($"User profile '{userId}' was not found.", problemDetails.Detail);
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
