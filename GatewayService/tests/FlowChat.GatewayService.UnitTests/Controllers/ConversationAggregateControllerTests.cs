using System.Security.Claims;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Controllers;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;
using FlowChat.GatewayService.Api.Models;
using FlowChat.GatewayService.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.GatewayService.UnitTests.Controllers;

public sealed class ConversationAggregateControllerTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IChatServiceClient> _chatClientMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<ConversationAggregateMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task OpenDuetConversation_ChatServiceResponse_MapsSequenceContract()
    {
        var userId = _fixture.Create<Guid>();
        var partnerUserId = _fixture.Create<Guid>();
        var conversationId = _fixture.Create<Guid>();
        var message = CreateMessage(conversationId);
        var conversation = new DuetConversationClientDto(conversationId, []);
        var messages = new GetConversationMessagesResult(
            [message],
            NextBeforeSequenceNum: 37,
            CurrentSequenceNum: 84,
            HasMore: true);

        _chatClientMock
            .Setup(x => x.GetDuetConversationAsync(partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        SetupMessages(messages);

        var controller = CreateController(userId);

        var result = await controller.OpenDuetConversation(
            new OpenDuetConversationRequest(partnerUserId, null),
            CancellationToken.None);

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<OpenDuetConversationResponse>().Subject;
        response.NextBeforeSequenceNum.Should().Be(37);
        response.CurrentSequenceNum.Should().Be(84);
        response.Messages.Should().ContainSingle()
            .Which.SequenceNum.Should().Be(message.SequenceNum);
    }

    [Fact]
    public async Task OpenGroupConversation_ChatServiceResponse_MapsSequenceContract()
    {
        var userId = _fixture.Create<Guid>();
        var conversationId = _fixture.Create<Guid>();
        var message = CreateMessage(conversationId);
        var conversation = new GroupConversationClientDto(conversationId, _fixture.Create<string>(), []);
        var messages = new GetConversationMessagesResult(
            [message],
            NextBeforeSequenceNum: 12,
            CurrentSequenceNum: 25,
            HasMore: true);

        _chatClientMock
            .Setup(x => x.GetGroupConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        SetupMessages(messages);

        var controller = CreateController(userId);

        var result = await controller.OpenGroupConversation(
            new OpenGroupConversationRequest(conversationId),
            CancellationToken.None);

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<OpenGroupConversationResponse>().Subject;
        response.NextBeforeSequenceNum.Should().Be(12);
        response.CurrentSequenceNum.Should().Be(25);
        response.Messages.Should().ContainSingle()
            .Which.SequenceNum.Should().Be(message.SequenceNum);
    }

    private ConversationAggregateController CreateController(Guid userId)
    {
        var controller = new ConversationAggregateController(
            _chatClientMock.Object,
            _mediatorMock.Object,
            _mapper,
            NullLogger<ConversationAggregateController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity([new Claim("sub", userId.ToString())], "Test"))
            }
        };
        return controller;
    }

    private void SetupMessages(GetConversationMessagesResult messages) =>
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<GetConversationMessagesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<GetConversationMessagesResult>.Success(messages));

    private ChatMessageClientDto CreateMessage(Guid conversationId) =>
        new(
            _fixture.Create<Guid>(),
            conversationId,
            _fixture.Create<Guid>(),
            _fixture.Create<string>(),
            _fixture.Create<DateTimeOffset>(),
            _fixture.Create<long>());
}
