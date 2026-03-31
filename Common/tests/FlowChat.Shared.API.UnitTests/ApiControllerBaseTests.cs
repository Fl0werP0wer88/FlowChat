using FlowChat.Shared.API;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowChat.Shared.API.UnitTests;

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

        var objectResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatusCode);
        problemDetails.Status.Should().Be(expectedStatusCode);
        problemDetails.Detail.Should().Be(error.ErrorMessage);
    }

    [Fact]
    public void HandleError_UsesJoinedValidationErrors_AsProblemDetail()
    {
        var controller = CreateController();
        var error = DomainError.Validation("Validation failed.", ["first issue", "second issue"]);

        var result = controller.InvokeHandleError(error);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Be("first issue,second issue");
    }

    [Fact]
    public void DomainError_Unauthorized_UsesUnauthorizedErrorType()
    {
        var error = DomainError.Unauthorized("Unauthorized detail");

        error.ErrorType.Should().Be(ErrorType.Unauthorized);
        error.ErrorMessage.Should().Be("Unauthorized detail");
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
