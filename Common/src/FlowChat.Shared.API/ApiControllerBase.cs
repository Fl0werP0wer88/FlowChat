using FlowChat.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.Shared.API;

public abstract class ApiControllerBase : ControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly Dictionary<ErrorType, Func<string?, IEnumerable<string>?, bool, ObjectResult>> _errorHandlers;
    private readonly Func<string?>? _internalApiKeyAccessor;

    protected ApiControllerBase() : this(null)
    {
    }

    protected ApiControllerBase(Func<string?>? internalApiKeyAccessor)
    {
        _internalApiKeyAccessor = internalApiKeyAccessor;
        _errorHandlers = new Dictionary<ErrorType, Func<string?, IEnumerable<string>?, bool, ObjectResult>>
        {
            { ErrorType.Conflict, ConflictResponse },
            { ErrorType.NotFound, NotFoundResponse },
            { ErrorType.BadRequest, BadRequestResponse },
            { ErrorType.Validation, ValidationResponse },
            { ErrorType.Unauthorized, UnauthorizedResponse },
            { ErrorType.Unexpected, UnexpectedResponse }
        };
    }

    protected ObjectResult HandleError(IDomainError error)
    {
        if (_errorHandlers.TryGetValue(error.ErrorType, out var handler))
        {
            return handler(error.ErrorMessage, error.Errors, error.IsTransient);
        }

        throw new InvalidOperationException($"Unsupported error type: {error.ErrorType}");
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
        bool isTransient = false) =>
        NotFound(ProblemDetailsFactory.CreateNotFound(HttpContext, details, errors, isTransient));

    protected ObjectResult BadRequestResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        bool isTransient = false) =>
        BadRequest(ProblemDetailsFactory.CreateBadRequest(HttpContext, details, errors, isTransient));

    protected ObjectResult ConflictResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        bool isTransient = false) =>
        Conflict(ProblemDetailsFactory.CreateConflict(HttpContext, details, errors, isTransient));

    protected ObjectResult ValidationResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        bool isTransient = false) =>
        BadRequest(ProblemDetailsFactory.CreateValidation(HttpContext, details, errors, isTransient));

    protected ObjectResult UnauthorizedResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        bool isTransient = false) =>
        Unauthorized(ProblemDetailsFactory.CreateUnauthorized(HttpContext, details, errors, isTransient));

    protected ObjectResult UnexpectedResponse(
        string? details = null,
        IEnumerable<string>? errors = null,
        bool isTransient = false) =>
        StatusCode(
            StatusCodes.Status500InternalServerError,
            ProblemDetailsFactory.CreateUnexpected(HttpContext, details, errors, isTransient));
}

