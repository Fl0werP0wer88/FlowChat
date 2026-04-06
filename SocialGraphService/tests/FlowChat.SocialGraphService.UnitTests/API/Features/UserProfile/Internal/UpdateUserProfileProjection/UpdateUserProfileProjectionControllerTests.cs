using AutoFixture;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UpdateUserProfileProjection;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UpdateUserProfileProjectionControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Update_WhenApiKeyMatches_SendsCommandAndReturnsAccepted()
    {
        UpdateUserProfileProjectionCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (UpdateUserProfileProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Update(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = " jane.doe ",
                DisplayName = " Jane Doe ",
                Bio = " updated "
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.FriendlyUserId.Should().Be(" jane.doe ");
        capturedCommand.DisplayName.Should().Be(" Jane Doe ");
        capturedCommand.Bio.Should().Be(" updated ");
    }

    [Fact]
    public async Task Update_WhenProjectionDoesNotExist_ReturnsNotFound()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.NotFound("User profile projection was not found.")));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Update(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = "jane.doe",
                DisplayName = "Jane Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.Validation(errors: ["Payload does not contain valid FriendlyUserId."])));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Update(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = string.Empty,
                DisplayName = "Jane Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static UpdateUserProfileProjectionController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new UpdateUserProfileProjectionController(
            mediatorMock.Object,
            InternalUserProfileProjectionControllerTestFactory.CreateApiSettingsManager(expectedApiKey).Object);

        InternalUserProfileProjectionControllerTestFactory.ConfigureControllerContext(controller, providedApiKey);
        return controller;
    }
}
