using AutoMapper;
using FluentAssertions;
using FlowChat.ChatService.Api.Features.ChatMessage.Internal.GetConversationMessagesRangeAscending;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeAscending;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.ChatMessage.Internal.GetConversationMessagesRangeAscending;

public sealed class GetConversationMessagesRangeAscendingControllerTests
{
    private const string ApiKey = "test-key";
    private readonly Mock<IMediator> _mediator = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GetConversationMessagesRangeAscendingMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task GetRange_ValidApiKey_MapsQueryAndResponse()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var page = new ConversationMessagesRangeAscendingPageDto(
            [new(Guid.NewGuid(), conversationId, userId, "text", DateTimeOffset.UtcNow, 4)],
            3,
            7,
            9,
            true);
        _mediator
            .Setup(x => x.Send(
                new GetConversationMessagesRangeAscendingQuery(conversationId, userId, 3, 7, 25),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeAscendingPageDto>.Success(page));
        var controller = CreateController(withApiKey: true);

        var result = await controller.GetRange(
            conversationId, userId, 3, 7, 25, CancellationToken.None);

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<GetConversationMessagesRangeAscendingResponse>().Subject;
        response.StartSequenceNum.Should().Be(3);
        response.Items.Should().ContainSingle().Which.SequenceNum.Should().Be(4);
    }

    private GetConversationMessagesRangeAscendingController CreateController(bool withApiKey)
    {
        var controller = new GetConversationMessagesRangeAscendingController(
            _mediator.Object,
            _mapper,
            Options.Create(new InternalApiSettingsSection { ApiKey = ApiKey }));
        var context = new DefaultHttpContext();
        if (withApiKey)
        {
            context.Request.Headers["X-Internal-Api-Key"] = ApiKey;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
