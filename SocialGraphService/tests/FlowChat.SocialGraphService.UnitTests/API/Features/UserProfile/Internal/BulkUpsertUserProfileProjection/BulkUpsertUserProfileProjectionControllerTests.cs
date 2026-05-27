using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class BulkUpsertUserProfileProjectionControllerTests
{
    [Fact]
    public async Task BulkUpsert_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediatorMock);

        var result = await controller.BulkUpsert(
            new BulkUpsertUserProfileProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task BulkUpsert_WhenApiKeyMatches_SendsCommandAndReturnsOkResponse()
    {
        BulkUpsertUserProfileProjectionCommand? capturedCommand = null;
        var userProfileId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<BulkUpsertCommandResult>>, CancellationToken>(
                (request, _) => capturedCommand = (BulkUpsertUserProfileProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(new BulkUpsertCommandResult(1, 1)));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsert(
            new BulkUpsertUserProfileProjectionRequest
            {
                Items =
                [
                    new UserProfileProjectionRequest
                    {
                        UserProfileId = userProfileId,
                        FriendlyUserId = " user-1 ",
                        MainEmailAddress = " user@example.com ",
                        MainEmailIsConfirmed = true,
                        MainEmailIsVisible = true,
                        IsActive = true
                    }
                ]
            },
            CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BulkUpsertUserProfileProjectionResponse>().Subject;
        response.RequestedCount.Should().Be(1);
        response.UpsertedCount.Should().Be(1);
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().ContainSingle().Which.Should().Be(
            new BulkUpsertUserProfileProjectionCommandItem(
                userProfileId,
                " user-1 ",
                null,
                null,
                null,
                " user@example.com ",
                true,
                true,
                null,
                null,
                null,
                null,
                null,
                true,
                null));
    }

    [Fact]
    public async Task BulkUpsert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Failure(
                DomainError.Validation(errors: ["Items must contain at least one item."])));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsert(
            new BulkUpsertUserProfileProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static BulkUpsertUserProfileProjectionController CreateController(
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new BulkUpsertUserProfileProjectionController(
            mediatorMock.Object,
            InternalUserProfileProjectionControllerTestFactory.CreateApiSettings("expected-key"));

        InternalUserProfileProjectionControllerTestFactory.ConfigureControllerContext(controller, providedApiKey);
        return controller;
    }
}
