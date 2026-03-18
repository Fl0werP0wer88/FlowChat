using System.Net;
using System.Text.Json;
using FlowChat.RealtimeService.Application.Realtime.Contracts;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.RealtimeService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeInternalApiClientTests
{
    [Fact]
    public async Task PublishMessageAsync_PostsToExpectedEndpointWithApiKey()
    {
        string? requestBody = null;
        var handler = new CapturingHttpMessageHandler(async (request, _) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RealtimeApi:BaseUrl"] = "http://localhost:5215",
                ["RealtimeApi:ApiKey"] = "internal-key"
            })
            .Build();
        var client = new RealtimeInternalApiClient(httpClient, new ApiSettingsManager(configuration));

        await client.PublishMessageAsync(
            new PublishMessageRequest
            {
                MessageId = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                SenderUserId = Guid.NewGuid(),
                SenderDisplayName = "John Doe",
                Text = "Hello",
                SentAtUtc = new DateTime(2026, 3, 17, 9, 0, 0, DateTimeKind.Utc),
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("http://localhost:5215/internal/realtime/messages", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("internal-key", handler.LastRequest.Headers.GetValues(RealtimeInternalApiClient.ApiKeyHeaderName).Single());

        var payload = JsonSerializer.Deserialize<PublishMessageRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal("John Doe", payload!.SenderDisplayName);
    }
}
