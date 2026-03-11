using FlowChat.Domain.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles;

public abstract class UserProfilesControllerBase : ControllerBase
{
    protected ObjectResult CreateErrorResponse(IDomainError error)
    {
        var statusCode = error.ErrorType switch
        {
            var type when type == ErrorType.NotFound => StatusCodes.Status404NotFound,
            var type when type == ErrorType.Conflict => StatusCodes.Status409Conflict,
            var type when type == ErrorType.BadRequest || type == ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, detail: error.ErrorMessage);
    }
}
