using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FlowChat.GatewayService.Api.Features.Gateway.Public.GetGatewayHealth;
using FlowChat.GatewayService.Api.Features.Gateway.Public.GetGatewayRoutes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FlowChat.GatewayService.IntegrationTests.API;

public sealed class GatewayFeatureRoutesTests(GatewayApiFactory factory)
    : IClassFixture<GatewayApiFactory>
{
    [Fact]
    public async Task HealthRoute_RemainsAnonymousAndReturnsStatus()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/gateway/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetGatewayHealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task RoutesRoute_AuthenticatedUser_ReturnsCatalog()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            Guid.NewGuid().ToString("D"));

        var response = await client.GetAsync("/gateway/routes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<GetGatewayRoutesResponse>>();
        body.Should().NotBeNull();
        body.Should().NotBeEmpty();
    }
}
