using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfileByEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfileByFriendlyUserId;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfile;

[ApiController]
[Authorize]
[Route("api/userprofiles")]
public sealed class UserProfilesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UserProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(GetUserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserProfileQuery(userId), cancellationToken);

        return result.IsSuccess
            ? Ok(new GetUserProfileResponse(MapToResponse(result.Value)))
            : HandleError(result.Error);
    }

    [HttpGet("by-email")]
    [ProducesResponseType(typeof(GetUserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEmail(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserProfileByEmailQuery(email), cancellationToken);

        return result.IsSuccess
            ? Ok(new GetUserProfileResponse(MapToResponse(result.Value)))
            : HandleError(result.Error);
    }

    [HttpGet("by-friendly-id/{friendlyUserId}")]
    [ProducesResponseType(typeof(GetUserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByFriendlyUserId(
        [FromRoute] string friendlyUserId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserProfileByFriendlyUserIdQuery(friendlyUserId), cancellationToken);

        return result.IsSuccess
            ? Ok(new GetUserProfileResponse(MapToResponse(result.Value)))
            : HandleError(result.Error);
    }

    private static UserProfileResponse MapToResponse(UserProfileDto userProfile) =>
        new(
            userProfile.Id,
            userProfile.FriendlyUserId,
            userProfile.FirstName,
            userProfile.LastName,
            userProfile.Organization,
            userProfile.AvatarUrl,
            userProfile.Bio,
            userProfile.IsActive,
            userProfile.LastSeenAtUtc,
            [.. userProfile.Emails.Select(email => new EmailResponse(
                email.Id, email.Address, email.IsMain, email.IsAuth, email.IsConfirmed, email.IsVisible))],
            [.. userProfile.Phones.Select(phone => new PhoneResponse(
                phone.Id, phone.Number, phone.IsMain, phone.IsConfirmed, phone.IsVisible))]);
}
