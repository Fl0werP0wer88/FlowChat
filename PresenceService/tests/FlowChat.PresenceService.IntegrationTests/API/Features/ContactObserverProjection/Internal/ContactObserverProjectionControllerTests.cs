using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.IntegrationTests.API.Features.ContactObserverProjection.Internal;

public sealed class ContactObserverProjectionControllerTests(PresenceApiFactory factory)
    : IClassFixture<PresenceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task BulkUpsert_WithValidInternalApiKey_PersistsProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/contact-observers/bulk-upsert")
        {
            Content = JsonContent.Create(new
            {
                Items = new[]
                {
                    new { ObservedUserId = observedUserId, ObserverUserId = observerUserId }
                }
            })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var exists = await factory.WithDbContextAsync(db => db.ContactObserverProjections.AnyAsync(
            x => x.ObservedUserId == observedUserId && x.ObserverUserId == observerUserId));
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_WithValidInternalApiKey_RemovesProjectionAndIsIdempotent()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();

        await factory.WithDbContextAsync(async db =>
        {
            await db.ContactObserverProjections.AddAsync(new PresenceService.Persistence.Entities.ContactObserverReadModelEntity
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                CreatedBy = "test",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                LastModifiedBy = "test",
                LastModifiedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var request = new HttpRequestMessage(HttpMethod.Delete, "/internal/presence/contact-observers/delete")
        {
            Content = JsonContent.Create(new { ObservedUserId = observedUserId, ObserverUserId = observerUserId })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var exists = await factory.WithDbContextAsync(db => db.ContactObserverProjections.AnyAsync(
            x => x.ObservedUserId == observedUserId && x.ObserverUserId == observerUserId));
        exists.Should().BeFalse();
    }
}
