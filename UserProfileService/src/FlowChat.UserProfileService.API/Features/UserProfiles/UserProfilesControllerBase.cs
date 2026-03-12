using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles;

public abstract class UserProfilesControllerBase : ControllerBase
{
    private readonly Dictionary<ErrorType, Func<string?, IEnumerable<string>?, ObjectResult>> _errorHandlers;

    protected UserProfilesControllerBase()
    {
        _errorHandlers = new Dictionary<ErrorType, Func<string?, IEnumerable<string>?, ObjectResult>>
        {
            { ErrorType.Conflict, ConflictResponse },
            { ErrorType.NotFound, NotFoundResponse },
            { ErrorType.BadRequest, BadRequestResponse },
            { ErrorType.Validation, ValidationResponse },
            { ErrorType.Unexpected, UnexpectedResponse }
        };
    }

    protected ObjectResult CreateErrorResponse(IDomainError error)
    {
        return HandleError(error);
    }

    protected ObjectResult HandleError(IDomainError error)
    {
        if (_errorHandlers.TryGetValue(error.ErrorType, out var handler))
        {
            return handler(error.ErrorMessage, error.Errors);
        }

        throw new InvalidOperationException($"Unsupported error type: {error.ErrorType}");
    }

    protected ObjectResult NotFoundResponse(string? details = null, IEnumerable<string>? errors = null) =>
        NotFound(ProblemDetailsFactory.CreateNotFound(HttpContext, details, errors));

    protected ObjectResult BadRequestResponse(string? details = null, IEnumerable<string>? errors = null) =>
        BadRequest(ProblemDetailsFactory.CreateBadRequest(HttpContext, details, errors));

    protected ObjectResult ConflictResponse(string? details = null, IEnumerable<string>? errors = null) =>
        Conflict(ProblemDetailsFactory.CreateConflict(HttpContext, details, errors));

    protected ObjectResult ValidationResponse(string? details = null, IEnumerable<string>? errors = null) =>
        BadRequest(ProblemDetailsFactory.CreateValidation(HttpContext, details, errors));

    protected ObjectResult UnexpectedResponse(string? details = null, IEnumerable<string>? errors = null) =>
        StatusCode(
            StatusCodes.Status500InternalServerError,
            ProblemDetailsFactory.CreateUnexpectedResponse(HttpContext, details, errors));
}
