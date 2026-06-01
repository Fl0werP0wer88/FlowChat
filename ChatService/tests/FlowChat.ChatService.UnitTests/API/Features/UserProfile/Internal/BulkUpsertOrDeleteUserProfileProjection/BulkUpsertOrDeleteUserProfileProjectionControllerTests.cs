using FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionControllerTests
{
    private const string ApiKey = "test-api-key";

    private readonly Mock<IMediator> _mediatorMock = new();

    private BulkUpsertOrDeleteUserProfileProjectionController CreateController()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Internal-Api-Key"] = ApiKey;

        return new BulkUpsertOrDeleteUserProfileProjectionController(
            _mediatorMock.Object,
            Options.Create(new InternalApiSettingsSection { ApiKey = ApiKey }))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WhenCommandSucceeds_Returns204NoContent()
    {
        var userProfileId = Guid.NewGuid();
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((command, _) => capturedCommand = (BulkUpsertOrDeleteUserProfileProjectionCommand)command)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var result = await CreateController().BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserProfileProjectionRequest
            {
                Items =
                [
                    new BulkUpsertOrDeleteUserProfileProjectionRequestItem
                    {
                        UserProfileId = userProfileId,
                        SourceVersion = 7,
                        Value = new BulkUpsertOrDeleteUserProfileProjectionRequestValue
                        {
                            FriendlyUserId = "jdoe",
                            FirstName = "John",
                            Source = "consumer"
                        }
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().ContainSingle();
    }
}
