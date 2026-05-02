using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowChat.Core.Domain;
using FlowChat.PresenceService.API.Features.Presence.Internal.GetPresenceStatusesBatch;

namespace FlowChat.PresenceService.IntegrationTests.API.Features.Presence.Internal;

public sealed class GetPresenceStatusesBatchControllerTests(PresenceApiFactory factory)
    : IClassFixture<PresenceApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetStatuses_WithValidInternalApiKey_ReturnsStoredAndInvisibleStatuses()
    {
        var activeUserId = Guid.NewGuid();
        var missingUserId = Guid.NewGuid();
        var changedAtUtc = new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero);
        await factory.PresenceStatusStore.SetAsync(
            activeUserId,
            PresenceStatus.Active,
            changedAtUtc,
            CancellationToken.None);

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/statuses/batch")
        {
            Content = JsonContent.Create(new { UserIds = new[] { activeUserId, missingUserId } })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statuses = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<ContactPresenceStatusResponse>>(JsonOptions);
        statuses.Should().BeEquivalentTo([
            new ContactPresenceStatusResponse
            {
                UserId = activeUserId,
                Status = PresenceStatus.Active,
                ChangedAtUtc = changedAtUtc
            },
            new ContactPresenceStatusResponse
            {
                UserId = missingUserId,
                Status = PresenceStatus.Invisible,
                ChangedAtUtc = DateTimeOffset.MinValue
            }
        ]);
    }

    [Fact]
    public async Task GetStatuses_WithoutInternalApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/internal/presence/statuses/batch",
            new { UserIds = new[] { Guid.NewGuid() } });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
