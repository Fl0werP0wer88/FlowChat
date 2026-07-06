using System.Reflection;
using FlowChat.ChatService.Api.Features.Conversation.Public.AddGroupParticipants;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
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

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.AddGroupParticipants;

public sealed class AddGroupParticipantsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private AddGroupParticipantsController CreateController() =>
        new(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };

    [Fact]
    public void AddGroupParticipants_HasGroupRouteAndRequiresAuthorization()
    {
        var controllerType = typeof(AddGroupParticipantsController);
        var actionMethod = controllerType.GetMethod(nameof(AddGroupParticipantsController.AddGroupParticipants));

        controllerType.GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/conversations/group/{conversationId:guid}/participants");
        controllerType.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        actionMethod.Should().NotBeNull();
        actionMethod!.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(attribute => attribute.StatusCode == StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task AddGroupParticipants_WhenParticipantsNewlyAdded_Returns202Accepted()
    {
        var conversationId = Guid.NewGuid();
        var participantId1 = Guid.NewGuid();
        var participantId2 = Guid.NewGuid();
        AddGroupParticipantsCommand? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddGroupParticipantsCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((cmd, _) => capturedCommand = (AddGroupParticipantsCommand)cmd)
            .ReturnsAsync(FlowChatResult<bool>.Success(true));

        var controller = CreateController();

        var actionResult = await controller.AddGroupParticipants(
            conversationId,
            new AddGroupParticipantsRequest { ParticipantUserIds = [participantId1, participantId2] },
            CancellationToken.None);

        actionResult.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(conversationId);
        capturedCommand.ParticipantUserIds.Should().BeEquivalentTo([participantId1, participantId2]);
    }

    [Fact]
    public async Task AddGroupParticipants_WhenAllParticipantsAlreadyExist_Returns200Ok()
    {
        var conversationId = Guid.NewGuid();

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddGroupParticipantsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<bool>.Success(false));

        var actionResult = await CreateController().AddGroupParticipants(
            conversationId,
            new AddGroupParticipantsRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task AddGroupParticipants_WhenConversationNotFound_ReturnsNotFound()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddGroupParticipantsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<bool>.Failure(
                DomainError.NotFound("Conversation not found.")));

        var actionResult = await CreateController().AddGroupParticipants(
            Guid.NewGuid(),
            new AddGroupParticipantsRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task AddGroupParticipants_WhenDuetConversation_ReturnsBadRequest()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddGroupParticipantsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<bool>.Failure(
                DomainError.BadRequest("Cannot add participants to a one-on-one conversation.")));

        var actionResult = await CreateController().AddGroupParticipants(
            Guid.NewGuid(),
            new AddGroupParticipantsRequest { ParticipantUserIds = [Guid.NewGuid()] },
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
