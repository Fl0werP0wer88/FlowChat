using FlowChat.Shared.API;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfiles;

[ApiController]
[Authorize]
[Route("api/userprofiles")]
public sealed class SearchUserProfilesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SearchUserProfilesController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchUserProfilesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchUserProfilesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SearchUserProfilesQuery(
                request.FirstName,
                request.LastName,
                request.Organization),
            cancellationToken);

        return result.IsSuccess
            ? Ok(new SearchUserProfilesResponse([.. result.Value.Select(MapToResponse)]))
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
