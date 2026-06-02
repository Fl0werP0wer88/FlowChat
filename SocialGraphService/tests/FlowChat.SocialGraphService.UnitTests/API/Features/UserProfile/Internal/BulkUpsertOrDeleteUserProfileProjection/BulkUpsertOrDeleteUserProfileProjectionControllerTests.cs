using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class BulkUpsertOrDeleteUserProfileProjectionControllerTests
{
    [Fact]
    public async Task BulkUpsertOrDelete_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediatorMock);

        var result = await controller.BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserProfileProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WhenApiKeyMatches_SendsCommandAndReturnsNoContent()
    {
        BulkUpsertOrDeleteUserProfileProjectionCommand? capturedCommand = null;
        var userProfileId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>(
                (request, _) => capturedCommand = (BulkUpsertOrDeleteUserProfileProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsertOrDelete(
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
                            FriendlyUserId = " user-1 ",
                            MainEmailAddress = " user@example.com ",
                            MainEmailIsConfirmed = true,
                            MainEmailIsVisible = true,
                            IsActive = true,
                            Source = "user-profile-projection"
                        }
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        capturedCommand.Should().NotBeNull();
        var item = capturedCommand!.Items.Should().ContainSingle().Subject;
        item.EntityId.Should().Be(Id<UserProfileProjectionDto>.FromGuid(userProfileId));
        item.SourceVersion.Should().Be(7);
        item.Value.Should().NotBeNull();
        item.Value!.FriendlyUserId.Should().Be(" user-1 ");
        item.Value.MainEmail!.Address.Should().Be(" user@example.com ");
        item.Value.MainEmail.IsConfirmed.Should().BeTrue();
        item.Value.MainEmail.IsVisible.Should().BeTrue();
        item.Value.SourceVersion.Should().Be(7);
        item.Value.Source.Should().Be("user-profile-projection");
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WhenUserProfileIdIsEmpty_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserProfileProjectionRequest
            {
                Items =
                [
                    new BulkUpsertOrDeleteUserProfileProjectionRequestItem
                    {
                        UserProfileId = Guid.Empty,
                        SourceVersion = 1
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertOrDeleteUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.Validation(errors: ["Items must contain at least one item."])));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserProfileProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static BulkUpsertOrDeleteUserProfileProjectionController CreateController(
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new BulkUpsertOrDeleteUserProfileProjectionController(
            mediatorMock.Object,
            InternalUserProfileProjectionControllerTestFactory.CreateApiSettings("expected-key"));

        InternalUserProfileProjectionControllerTestFactory.ConfigureControllerContext(controller, providedApiKey);
        return controller;
    }
}
