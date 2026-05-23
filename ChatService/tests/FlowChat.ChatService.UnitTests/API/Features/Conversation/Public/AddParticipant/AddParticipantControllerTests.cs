using FlowChat.ChatService.Api.Features.Conversation.Public.AddParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.AddParticipant;

public sealed class AddParticipantControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private AddParticipantController CreateController() =>
        new(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };

    [Fact]
    public async Task AddParticipant_WhenParticipantsNewlyAdded_Returns202Accepted()
    {
        var conversationId = Guid.NewGuid();
        var participantId1 = Guid.NewGuid();
        var participantId2 = Guid.NewGuid();
        AddParticipantCommand? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddParticipantCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((cmd, _) => capturedCommand = (AddParticipantCommand)cmd)
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<bool>>.Success(
                new IdempotentCommandResult<bool>(true, WasAlreadyProcessed: false)));

        var controller = CreateController();

        var actionResult = await controller.AddParticipant(
            conversationId,
            new AddParticipantRequest { ParticipantUserIds = [participantId1, participantId2] },
            CancellationToken.None);

        actionResult.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(conversationId);
        capturedCommand.ParticipantUserIds.Should().BeEquivalentTo([participantId1, participantId2]);
    }

    [Fact]
    public async Task AddParticipant_WhenAllParticipantsAlreadyExist_Returns200Ok()
    {
        var conversationId = Guid.NewGuid();

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddParticipantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<bool>>.Success(
                new IdempotentCommandResult<bool>(false, WasAlreadyProcessed: false)));

        var actionResult = await CreateController().AddParticipant(
            conversationId,
            new AddParticipantRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task AddParticipant_WhenRaceConditionDetected_Returns200Ok()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddParticipantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<bool>>.Success(
                new IdempotentCommandResult<bool>(false, WasAlreadyProcessed: true)));

        var actionResult = await CreateController().AddParticipant(
            Guid.NewGuid(),
            new AddParticipantRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task AddParticipant_WhenConversationNotFound_ReturnsNotFound()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddParticipantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<bool>>.Failure(
                DomainError.NotFound("Conversation not found.")));

        var actionResult = await CreateController().AddParticipant(
            Guid.NewGuid(),
            new AddParticipantRequest { ParticipantUserIds = [Guid.NewGuid()] },
            CancellationToken.None);

        actionResult.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task AddParticipant_WhenDuetConversation_ReturnsBadRequest()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddParticipantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<bool>>.Failure(
                DomainError.BadRequest("Cannot add participants to a one-on-one conversation.")));

        var actionResult = await CreateController().AddParticipant(
            Guid.NewGuid(),
            new AddParticipantRequest { ParticipantUserIds = [Guid.NewGuid()] },
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
