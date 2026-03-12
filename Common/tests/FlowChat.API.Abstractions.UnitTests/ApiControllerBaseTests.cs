using FlowChat.API.Abstractions;
using FlowChat.Domain.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowChat.API.Abstractions.UnitTests;

public sealed class ApiControllerBaseTests
{
    public static TheoryData<IDomainError, int> ErrorCases => new()
    {
        { DomainError.Conflict("Conflict detail"), StatusCodes.Status409Conflict },
        { DomainError.NotFound("Not found detail"), StatusCodes.Status404NotFound },
        { DomainError.BadRequest("Bad request detail"), StatusCodes.Status400BadRequest },
        { DomainError.Validation("Validation detail"), StatusCodes.Status400BadRequest },
        { DomainError.Unauthorized("Unauthorized detail"), StatusCodes.Status401Unauthorized },
        { DomainError.UnExpected("Unexpected detail"), StatusCodes.Status500InternalServerError }
    };

    [Theory]
    [MemberData(nameof(ErrorCases))]
    public void HandleError_MapsEachErrorType_ToExpectedStatusCode(IDomainError error, int expectedStatusCode)
    {
        var controller = CreateController();

        var result = controller.InvokeHandleError(error);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(error.ErrorMessage, problemDetails.Detail);
    }

    [Fact]
    public void HandleError_UsesJoinedValidationErrors_AsProblemDetail()
    {
        var controller = CreateController();
        var error = DomainError.Validation("Validation failed.", ["first issue", "second issue"]);

        var result = controller.InvokeHandleError(error);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("first issue,second issue", problemDetails.Detail);
    }

    [Fact]
    public void DomainError_Unauthorized_UsesUnauthorizedErrorType()
    {
        var error = DomainError.Unauthorized("Unauthorized detail");

        Assert.Equal(ErrorType.Unauthorized, error.ErrorType);
        Assert.Equal("Unauthorized detail", error.ErrorMessage);
    }

    private static TestApiController CreateController()
    {
        var controller = new TestApiController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
            }
        };

        return controller;
    }

    private sealed class TestApiController : ApiControllerBase
    {
        public ObjectResult InvokeHandleError(IDomainError error) => HandleError(error);
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
