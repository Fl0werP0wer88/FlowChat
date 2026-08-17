using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.Contact.Public.GetContactsWithConversations;
using FlowChat.GatewayService.Api.Features.Conversation.Public.OpenDuetConversation;
using FlowChat.GatewayService.Api.Features.Conversation.Public.OpenGroupConversation;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.GatewayService.Infrastructure.Clients.PresenceService;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace FlowChat.GatewayService.IntegrationTests.API;

public sealed class AggregateRoutesTests(GatewayApiFactory factory)
    : IClassFixture<GatewayApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task ContactsRoute_IsHandledByContactSlice()
    {
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        factory.ChatServiceClient
            .Setup(x => x.GetDuetConversationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new DuetConversationListItemClientDto(
                    partnerId,
                    "partner",
                    null,
                    null,
                    Guid.NewGuid(),
                    2,
                    5,
                    false,
                    false,
                    false,
                    false)
            ]);
        factory.PresenceServiceClient
            .Setup(x => x.GetPresenceStatusesAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ContactPresenceStatusClientDto>
            {
                [partnerId] = new(partnerId, PresenceStatus.Active, DateTimeOffset.UtcNow)
            });
        using var client = CreateClient(userId);

        var response = await client.GetAsync("/api/aggregate/contacts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetContactsWithConversationsResponse>(
            JsonOptions);
        body.Should().NotBeNull();
        body!.Contacts.Should().ContainSingle().Which.UnreadCount.Should().Be(3);
    }

    [Fact]
    public async Task OpenDuetRoute_IsHandledByConversationSlice()
    {
        var userId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        factory.ChatServiceClient
            .Setup(x => x.GetDuetConversationAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DuetConversationClientDto(conversationId, []));
        SetupHistory(conversationId, userId, current: 4);
        using var client = CreateClient(userId);

        var response = await client.PutAsJsonAsync(
            "/api/aggregate/conversations/duet/open",
            new OpenDuetConversationRequest(partnerId, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OpenDuetConversationResponse>();
        body.Should().NotBeNull();
        body!.ConversationId.Should().Be(conversationId);
        body.CurrentSequenceNum.Should().Be(4);
    }

    [Fact]
    public async Task OpenGroupRoute_IsHandledByConversationSlice()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        factory.ChatServiceClient
            .Setup(x => x.GetGroupConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupConversationClientDto(conversationId, "group", []));
        SetupHistory(conversationId, userId, current: 7);
        using var client = CreateClient(userId);

        var response = await client.PutAsJsonAsync(
            "/api/aggregate/conversations/group/open",
            new OpenGroupConversationRequest(conversationId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OpenGroupConversationResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("group");
        body.CurrentSequenceNum.Should().Be(7);
    }

    private void SetupHistory(Guid conversationId, Guid userId, long current) =>
        factory.ChatServiceClient
            .Setup(x => x.GetConversationMessagesRangeDescendingAsync(
                conversationId,
                userId,
                1,
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new([], 1, current, current, false)));

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

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
