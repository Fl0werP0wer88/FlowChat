using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FlowChat.GatewayService.IntegrationTests.API;

public sealed class CorsPolicyTests(GatewayApiFactory factory)
    : IClassFixture<GatewayApiFactory>
{
    [Fact]
    public async Task PreflightRequest_ReactClientV2Origin_ReturnsCorsHeaders()
    {
        const string origin = "http://127.0.0.1:5173";
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/users/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(origin);
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().ContainSingle("true");
    }
}
