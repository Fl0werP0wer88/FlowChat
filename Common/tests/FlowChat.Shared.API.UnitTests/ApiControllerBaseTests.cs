using FlowChat.Core.Http;
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
        { DomainError.ConcurencyConflict("Concurrency conflict detail"), StatusCodes.Status409Conflict },
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
        problemDetails.Extensions.Should().ContainKey(ProblemDetailsExtensionNames.IsTransient);
        problemDetails.Extensions[ProblemDetailsExtensionNames.IsTransient].Should().Be(error.IsTransient);
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
    public void HandleError_WhenConcurrencyConflict_AddsConcurrencyErrorTag()
    {
        var controller = CreateController();

        var result = controller.InvokeHandleError(DomainError.ConcurencyConflict("Concurrency conflict detail"));

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var problemDetails = conflict.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Extensions["error"].Should().Be("concurrency_conflict");
    }

    [Fact]
    public void DomainError_Unauthorized_UsesUnauthorizedErrorType()
    {
        var error = DomainError.Unauthorized("Unauthorized detail");

        error.ErrorType.Should().Be(ErrorType.Unauthorized);
        error.ErrorMessage.Should().Be("Unauthorized detail");
    }

    [Fact]
    public void DomainError_ConcurencyConflict_UsesConcurencyConflictErrorType()
    {
        var error = DomainError.ConcurencyConflict("Concurrency conflict detail");

        error.ErrorType.Should().Be(ErrorType.ConcurencyConflict);
        error.ErrorMessage.Should().Be("Concurrency conflict detail");
    }

    [Fact]
    public void HasValidInternalApiKey_WhenAccessorIsNotConfigured_ReturnsFalse()
    {
        var controller = CreateController();

        var result = controller.InvokeHasValidInternalApiKey();

        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidInternalApiKey_WhenConfiguredKeyIsBlank_ReturnsFalse()
    {
        var controller = CreateController(() => " ");

        var result = controller.InvokeHasValidInternalApiKey();

        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidInternalApiKey_WhenHeaderIsMissing_ReturnsFalse()
    {
        var controller = CreateController(() => "expected-key");

        var result = controller.InvokeHasValidInternalApiKey();

        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidInternalApiKey_WhenHeaderDoesNotMatch_ReturnsFalse()
    {
        var controller = CreateController(() => "expected-key", "other-key");

        var result = controller.InvokeHasValidInternalApiKey();

        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidInternalApiKey_WhenHeaderMatches_ReturnsTrue()
    {
        var controller = CreateController(() => "expected-key", "expected-key");

        var result = controller.InvokeHasValidInternalApiKey();

        result.Should().BeTrue();
    }

    private static TestApiController CreateController(
        Func<string?>? internalApiKeyAccessor = null,
        string? providedApiKey = null)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        if (providedApiKey is not null)
        {
            httpContext.Request.Headers["X-Internal-Api-Key"] = providedApiKey;
        }

        var controller = internalApiKeyAccessor is null
            ? new TestApiController()
            : new TestApiController(internalApiKeyAccessor);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    private sealed class TestApiController : ApiControllerBase
    {
        public TestApiController()
        {
        }

        public TestApiController(Func<string?> internalApiKeyAccessor) : base(internalApiKeyAccessor)
        {
        }

        public ObjectResult InvokeHandleError(IDomainError error) => HandleError(error);

        public bool InvokeHasValidInternalApiKey() => HasValidInternalApiKey();
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
