using System.Net;
using AutoFixture;
using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Core.Exceptions;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace FlowChat.AuthService.UnitTests;

public sealed class AuthInternalApiClientTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task ChangeAuthEmailAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
    {
        var client = new AuthInternalApiClient(CreateHttpClient(HttpStatusCode.BadRequest));

        var act = () => client.ChangeAuthEmailAsync(
            new AuthEmailChangeRequest
            {
                UserId = _fixture.Create<Guid>(),
                EmailAddress = $"{_fixture.Create<string>()}@example.com"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenApiReturnsNotFound_ThrowsNonTransientException()
    {
        var client = new AuthInternalApiClient(CreateHttpClient(HttpStatusCode.NotFound));

        var act = () => client.ConfirmEmailAsync(
            new AuthEmailConfirmationRequest
            {
                EmailAddress = $"{_fixture.Create<string>()}@example.com"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
    {
        var client = new AuthInternalApiClient(CreateHttpClient(HttpStatusCode.BadRequest));

        var act = () => client.ConfirmEmailAsync(
            new AuthEmailConfirmationRequest
            {
                EmailAddress = $"{_fixture.Create<string>()}@example.com"
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>();
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("failure")
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:7236")
        };
    }
}
