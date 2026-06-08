using System.Security.Claims;
using FlowChat.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.Shared.API;

public abstract class ApiControllerBase : ControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly Dictionary<ErrorType, Func<string?, IEnumerable<string>?, FailureKind, ObjectResult>> _errorHandlers;
    private readonly Func<string?>? _internalApiKeyAccessor;

    protected ApiControllerBase() : this(null)
    {
    }

    protected ApiControllerBase(Func<string?>? internalApiKeyAccessor)
    {
        _internalApiKeyAccessor = internalApiKeyAccessor;
        _errorHandlers = new Dictionary<ErrorType, Func<string?, IEnumerable<string>?, FailureKind, ObjectResult>>
        {
            { ErrorType.Conflict, ConflictResponse },
            { ErrorType.NotFound, NotFoundResponse },
            { ErrorType.BadRequest, BadRequestResponse },
            { ErrorType.Validation, ValidationResponse },
            { ErrorType.Unauthorized, UnauthorizedResponse }
        };
    }

    protected ObjectResult HandleError(IDomainError error)
    {
        if (error.ErrorType == ErrorType.Unexpected)
        {
            return UnexpectedResponse(error.ErrorMessage, error.Errors, error.FailureKind);
        }

        if (_errorHandlers.TryGetValue(error.ErrorType, out var handler))
        {
            return handler(error.ErrorMessage, error.Errors, error.FailureKind);
        }

        throw new InvalidOperationException($"Unsupported error type: {error.ErrorType}");
    }

    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    protected bool HasValidInternalApiKey()
    {
        if (_internalApiKeyAccessor is null)
        {
            return false;
        }

        var expectedApiKey = _internalApiKeyAccessor();
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return false;
        }

        if (!Request.Headers.TryGetValue(InternalApiKeyHeaderName, out var providedApiKey))
        {
            return false;
        }

        return string.Equals(providedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal);
    }

    protected ObjectResult NotFoundResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        NotFound(ProblemDetailsFactory.CreateNotFound(HttpContext, details, errors, failureKind));

    protected ObjectResult BadRequestResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        BadRequest(ProblemDetailsFactory.CreateBadRequest(HttpContext, details, errors, failureKind));

    protected ObjectResult ConflictResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        Conflict(ProblemDetailsFactory.CreateConflict(HttpContext, details, errors, failureKind));

    protected ObjectResult ValidationResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        BadRequest(ProblemDetailsFactory.CreateValidation(HttpContext, details, errors, failureKind));

    protected ObjectResult UnauthorizedResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        Unauthorized(ProblemDetailsFactory.CreateUnauthorized(HttpContext, details, errors, failureKind));

    protected ObjectResult UnexpectedResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        FailureKind failureKind = FailureKind.None) =>
        StatusCode(
            StatusCodes.Status500InternalServerError,
            ProblemDetailsFactory.CreateUnexpected(HttpContext, details, errors, failureKind));
}

