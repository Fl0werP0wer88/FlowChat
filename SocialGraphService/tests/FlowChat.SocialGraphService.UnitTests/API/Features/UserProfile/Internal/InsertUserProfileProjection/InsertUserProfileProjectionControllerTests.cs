using AutoFixture;
using FlowChat.Core.Results;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.InsertUserProfileProjection;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
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
    public async Task Insert_WhenApiKeyMatches_SendsCommandAndReturns201Created()
    {
        InsertUserProfileProjectionCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<InsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<IdempotentCommandResult<Unit>>>, CancellationToken>(
                (request, _) => capturedCommand = (InsertUserProfileProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Unit>>.Success(
                new IdempotentCommandResult<Unit>(Unit.Value, WasAlreadyProcessed: false)));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = " jdoe ",
                MainEmailAddress = " john@example.com ",
                MainEmailIsConfirmed = true,
                MainEmailIsVisible = true
            },
            CancellationToken.None);

        result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        capturedCommand.Should().NotBeNull();
        capturedCommand!.FriendlyUserId.Should().Be(" jdoe ");
        capturedCommand.MainEmailAddress.Should().Be(" john@example.com ");
        capturedCommand.MainEmailIsConfirmed.Should().BeTrue();
        capturedCommand.MainEmailIsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task Insert_WhenProjectionAlreadyProcessed_ReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<InsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Unit>>.Success(
                new IdempotentCommandResult<Unit>(Unit.Value, WasAlreadyProcessed: true)));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Insert(
            new UserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = "jdoe"
            },
            CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Insert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<InsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Unit>>.Failure(
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
            InternalUserProfileProjectionControllerTestFactory.CreateApiSettings(expectedApiKey));

        InternalUserProfileProjectionControllerTestFactory.ConfigureControllerContext(controller, providedApiKey);
        return controller;
    }
}
