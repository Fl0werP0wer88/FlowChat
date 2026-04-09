using AutoFixture;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.InsertUserProfileProjection;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class InsertUserProfileProjectionControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Insert_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController("expected-key", mediatorMock);

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = "jdoe"
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Insert_WhenApiKeyMatches_SendsCommandAndReturnsAccepted()
    {
        InsertUserProfileProjectionCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<InsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (InsertUserProfileProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = " jdoe ",
                MainEmail = " john@example.com "
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.FriendlyUserId.Should().Be(" jdoe ");
        capturedCommand.MainEmail.Should().Be(" john@example.com ");
    }

    [Fact]
    public async Task Insert_WhenProjectionAlreadyExists_ReturnsConflict()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<InsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.Conflict("User profile projection already exists.")));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = "jdoe"
            },
            CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Insert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<InsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.Validation(errors: ["Payload does not contain valid UserProfileId."])));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = Guid.Empty,
                FriendlyUserId = "jdoe"
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static InsertUserProfileProjectionController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new InsertUserProfileProjectionController(
            mediatorMock.Object,
            InternalUserProfileProjectionControllerTestFactory.CreateApiSettingsManager(expectedApiKey).Object);

        InternalUserProfileProjectionControllerTestFactory.ConfigureControllerContext(controller, providedApiKey);
        return controller;
    }
}
