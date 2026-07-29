using AutoMapper;
using FluentAssertions;
using FlowChat.ChatService.Api.Features.ChatMessage.Internal.GetConversationMessagesRangeDescending;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.ChatMessage.Internal.GetConversationMessagesRangeDescending;

public sealed class GetConversationMessagesRangeDescendingControllerTests
{
    [Fact]
    public async Task GetRange_MissingApiKey_ReturnsUnauthorizedWithoutDispatchingQuery()
    {
        var mediator = new Mock<IMediator>();
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<GetConversationMessagesRangeDescendingMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        var controller = new GetConversationMessagesRangeDescendingController(
            mediator.Object,
            mapper,
            Options.Create(new InternalApiSettingsSection { ApiKey = "test-key" }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetRange(
            Guid.NewGuid(), Guid.NewGuid(), null, null, 100, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        mediator.Verify(
            x => x.Send(
                It.IsAny<GetConversationMessagesRangeDescendingQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
