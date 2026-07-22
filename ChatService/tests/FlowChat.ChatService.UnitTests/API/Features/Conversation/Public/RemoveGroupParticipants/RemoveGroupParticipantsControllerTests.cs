using System.Reflection;
using FlowChat.ChatService.Api.Features.Conversation.Public.RemoveGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private RemoveGroupParticipantsController CreateController() =>
        new(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };

    [Fact]
    public void RemoveGroupParticipants_HasGroupRouteAndRequiresAuthorization()
    {
        var controllerType = typeof(RemoveGroupParticipantsController);
        var actionMethod = controllerType.GetMethod(nameof(RemoveGroupParticipantsController.RemoveGroupParticipants));

        controllerType.GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/conversations/group/{conversationId:guid}/participants");
        controllerType.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        actionMethod.Should().NotBeNull();
        actionMethod!.GetCustomAttribute<HttpDeleteAttribute>().Should().NotBeNull();
        actionMethod.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(attribute => attribute.StatusCode == StatusCodes.Status401Unauthorized);
        actionMethod.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(attribute => attribute.StatusCode == StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task RemoveGroupParticipants_WhenCommandSucceeds_Returns204NoContent()
    {
        var conversationId = Guid.NewGuid();
        var participantId1 = Guid.NewGuid();
        var participantId2 = Guid.NewGuid();
        RemoveGroupParticipantsCommandV2? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RemoveGroupParticipantsCommandV2>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((cmd, _) => capturedCommand = (RemoveGroupParticipantsCommandV2)cmd)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController();

        var actionResult = await controller.RemoveGroupParticipants(
            conversationId,
            new RemoveGroupParticipantsRequest { ParticipantUserIds = [participantId1, participantId2] },
            CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(conversationId);
        capturedCommand.ParticipantUserIds.Should().BeEquivalentTo([participantId1, participantId2]);
    }

    [Fact]
    public async Task RemoveGroupParticipants_WhenConversationNotFound_ReturnsNotFound()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RemoveGroupParticipantsCommandV2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.NotFound("Conversation not found.")));

        var actionResult = await CreateController().RemoveGroupParticipants(
            Guid.NewGuid(),
            new RemoveGroupParticipantsRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task RemoveGroupParticipants_WhenParticipantIsNotAMember_ReturnsBadRequest()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RemoveGroupParticipantsCommandV2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("User is not a participant in this conversation.")));

        var actionResult = await CreateController().RemoveGroupParticipants(
            Guid.NewGuid(),
            new RemoveGroupParticipantsRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task RemoveGroupParticipants_WhenRemovalWouldDropBelowMinimumParticipants_ReturnsBadRequest()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RemoveGroupParticipantsCommandV2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.BadRequest("Group conversations must have at least two participants.")));

        var actionResult = await CreateController().RemoveGroupParticipants(
            Guid.NewGuid(),
            new RemoveGroupParticipantsRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>();
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary) { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };
    }
}
