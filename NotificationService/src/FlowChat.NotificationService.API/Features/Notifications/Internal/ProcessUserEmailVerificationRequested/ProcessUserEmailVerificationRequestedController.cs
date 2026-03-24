using FlowChat.API.Abstractions;
using FlowChat.NotificationService.Application.Features.Notifications.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.NotificationService.Api.Features.Notifications.Internal.ProcessUserEmailVerificationRequested;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/notifications")]
public sealed class ProcessUserEmailVerificationRequestedController(
    IMediator mediator,
    IApiSettingsManager apiSettingsManager) : ApiControllerBase
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IApiSettingsManager _apiSettingsManager = apiSettingsManager
        ?? throw new ArgumentNullException(nameof(apiSettingsManager));

    [HttpPost("email-verification-requested")]
    public async Task<IActionResult> Process(
        [FromBody] ProcessUserEmailVerificationRequestedRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidInternalApiKey())
        {
            return Unauthorized();
        }

        if (request.UserId == Guid.Empty)
        {
            return BadRequestResponse("Payload does not contain valid UserId.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequestResponse("Payload does not contain valid Email.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequestResponse("Payload does not contain valid UserName.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmationLink))
        {
            return BadRequestResponse("Payload does not contain valid ConfirmationLink.");
        }

        var result = await _mediator.Send(
            new UserEmailVerificationRequestedCommand(
                request.UserId,
                request.Email.Trim(),
                request.UserName.Trim(),
                NormalizeDisplayName(request.DisplayName, request.UserName),
                request.ConfirmationLink.Trim(),
                NormalizeOptional(request.SourceMessageKey)),
            cancellationToken);

        return result.IsSuccess
            ? Accepted()
            : HandleError(result.Error);
    }

    private bool HasValidInternalApiKey()
    {
        var expectedApiKey = _apiSettingsManager.GetInternalApiSettings().ApiKey;
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

    private static string NormalizeDisplayName(string? displayName, string userName) =>
        string.IsNullOrWhiteSpace(displayName) ? userName.Trim() : displayName.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
