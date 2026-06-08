using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.IntegrationTests.API.Features.ContactObserverProjection.Internal;

public sealed class ContactObserverProjectionControllerTests(PresenceApiFactory factory)
    : IClassFixture<PresenceApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task BulkUpsertOrDelete_WithValidInternalApiKey_PersistsProjection()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/contact-observers/projection/bulk-upsert-or-delete")
        {
            Content = JsonContent.Create(new
            {
                Items = new[]
                {
                    new
                    {
                        ObservedUserId = observedUserId,
                        ObserverUserId = observerUserId,
                        SourceVersion = 1,
                        Value = new { Source = "integration-test" }
                    }
                }
            })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await factory.WithDbContextAsync(db => db.ContactObserverProjections.AnyAsync(
            x => x.ObservedUserId == observedUserId &&
                 x.ObserverUserId == observerUserId &&
                 x.SourceVersion == 1 &&
                 x.DeletedAt == null));
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WithDeleteItem_MarksProjectionAsDeleted()
    {
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();

        await factory.WithDbContextAsync(async db =>
        {
            await db.ContactObserverProjections.AddAsync(new PresenceService.Persistence.Entities.ContactObserverReadModelEntity
            {
                ObservedUserId = observedUserId,
                ObserverUserId = observerUserId,
                SourceVersion = 1
            });
            await db.SaveChangesAsync();
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/presence/contact-observers/projection/bulk-upsert-or-delete")
        {
            Content = JsonContent.Create(new
            {
                Items = new[]
                {
                    new
                    {
                        ObservedUserId = observedUserId,
                        ObserverUserId = observerUserId,
                        SourceVersion = 2,
                        Value = (object?)null
                    }
                }
            })
        };
        request.Headers.Add("X-Internal-Api-Key", PresenceApiFactory.InternalApiKey);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var projection = await factory.WithDbContextAsync(db => db.ContactObserverProjections.SingleAsync(
            x => x.ObservedUserId == observedUserId && x.ObserverUserId == observerUserId));
        projection.SourceVersion.Should().Be(2);
        projection.DeletedAt.Should().NotBeNull();
    }
}
