using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace FlowChat.GatewayService.IntegrationTests.API;

public sealed class ConversationMessagesRoutesTests(GatewayApiFactory factory)
    : IClassFixture<GatewayApiFactory>
{
    [Fact]
    public async Task HistoryRoute_IsHandledByGatewayMessageSlice()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        factory.ChatServiceClient
            .Setup(x => x.GetConversationMessagesRangeDescendingAsync(
                conversationId, userId, 1, null, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new([], 1, 7, 7, false)));
        using var client = CreateClient(userId);

        var response = await client.GetAsync(
            $"/api/chat/conversations/{conversationId}/messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetConversationMessagesResponse>();
        body.Should().NotBeNull();
        body!.CurrentSequenceNum.Should().Be(7);
    }

    [Fact]
    public async Task CatchUpRoute_IsHandledByGatewayMessageSlice()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        factory.ChatServiceClient
            .Setup(x => x.GetConversationMessagesRangeAscendingAsync(
                conversationId, userId, 6, null, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new([], 6, 9, 9, false)));
        using var client = CreateClient(userId);

        var response = await client.GetAsync(
            $"/api/chat/conversations/{conversationId}/messages/catch-up?afterSequenceNum=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CatchUpConversationMessagesResponse>();
        body.Should().NotBeNull();
        body!.ThroughSequenceNum.Should().Be(9);
    }

    private HttpClient CreateClient(Guid userId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            userId.ToString("D"));
        return client;
    }
}
