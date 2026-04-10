using System.Net;
using System.Net.Http.Json;
using FlowChat.Shared.Domain;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;
using FlowChat.UserProfileService.IntegrationTests.API;
using FlowChat.UserProfileService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.IntegrationTests.API.Features.UserProfile.Public.UpdateProfile;

public sealed class UpdateProfileControllerTests(UserProfileApiFactory factory)
    : IClassFixture<UserProfileApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UpdateProfile_WithValidRequest_Returns204AndPersistsChanges()
    {
        var userId = await CreateUserProfileAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/userprofiles/{userId}",
            new
            {
                FirstName = "John",
                LastName = "Doe",
                Organization = "FlowChat",
                AvatarUrl = "https://cdn.example/avatar.png",
                Bio = "about me",
                IsActive = false
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var typedUserId = Id<UserProfileAggregate>.FromGuid(userId);
        await factory.WithDbContextAsync(async dbContext =>
        {
            var profile = await dbContext.UserProfiles
                .AsNoTracking()
                .SingleAsync(x => x.Id.Equals(typedUserId));

            profile.FirstName.Should().Be("John");
            profile.LastName.Should().Be("Doe");
            profile.Organization.Should().Be("FlowChat");
            profile.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
            profile.Bio.Should().Be("about me");
            profile.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task UpdateProfile_WhenProfileDoesNotExist_Returns404NotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/userprofiles/{Guid.NewGuid()}",
            new
            {
                FirstName = "John",
                LastName = "Doe",
                Organization = "FlowChat",
                AvatarUrl = "https://cdn.example/avatar.png",
                Bio = "about me",
                IsActive = true
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProfile_WithTooLongFirstName_Returns400BadRequest()
    {
        var userId = await CreateUserProfileAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/userprofiles/{userId}",
            new
            {
                FirstName = new string('a', 101),
                LastName = "Doe",
                Organization = "FlowChat",
                AvatarUrl = "https://cdn.example/avatar.png",
                Bio = "about me",
                IsActive = true
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CreateUserProfileAsync()
    {
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            FriendlyUserId = $"updateuser-{userId:N}",
            Email = $"update_{userId:N}@example.com"
        };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/internal/userprofiles/initial")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Internal-Api-Key", UserProfileApiFactory.InternalApiKey);
        await _client.SendAsync(httpRequest);
        return userId;
    }
}
