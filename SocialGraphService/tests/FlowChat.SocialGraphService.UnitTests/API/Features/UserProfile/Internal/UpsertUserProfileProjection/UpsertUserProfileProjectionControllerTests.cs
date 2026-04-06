using AutoFixture;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UpsertUserProfileProjection;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UpsertUserProfileProjectionControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Upsert_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", new Mock<IUserProfileProjectionRepository>(), new Mock<IUnitOfWork>());

        var result = await controller.Upsert(
            new UpsertUserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Upsert_WhenApiKeyMatches_UpsertsProjectionAndSavesChanges()
    {
        UserProfileProjection? capturedProjection = null;
        var repositoryMock = new Mock<IUserProfileProjectionRepository>();
        repositoryMock
            .Setup(x => x.UpsertAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjection, CancellationToken>((projection, _) => capturedProjection = projection)
            .ReturnsAsync(true);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var controller = CreateController("expected-key", repositoryMock, unitOfWorkMock, "expected-key");

        var result = await controller.Upsert(
            new UpsertUserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = " jdoe ",
                DisplayName = " John Doe ",
                MainEmail = " john@example.com "
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedProjection.Should().NotBeNull();
        capturedProjection!.UserName.Should().Be("jdoe");
        capturedProjection.DisplayName.Should().Be("John Doe");
        capturedProjection.MainEmail.Should().Be("john@example.com");
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upsert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var controller = CreateController("expected-key", new Mock<IUserProfileProjectionRepository>(), new Mock<IUnitOfWork>(), "expected-key");

        var result = await controller.Upsert(
            new UpsertUserProfileProjectionRequest
            {
                UserProfileId = Guid.Empty,
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static UpsertUserProfileProjectionController CreateController(
        string expectedApiKey,
        Mock<IUserProfileProjectionRepository> repositoryMock,
        Mock<IUnitOfWork> unitOfWorkMock,
        string? providedApiKey = null)
    {
        var apiSettingsManagerMock = new Mock<IApiSettingsManager>();
        apiSettingsManagerMock
            .Setup(x => x.GetInternalApiSettings())
            .Returns(new InternalApiSettings { ApiKey = expectedApiKey });

        var controller = new UpsertUserProfileProjectionController(
            repositoryMock.Object,
            unitOfWorkMock.Object,
            apiSettingsManagerMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        if (providedApiKey is not null)
        {
            httpContext.Request.Headers["X-Internal-Api-Key"] = providedApiKey;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private sealed class SingleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType.IsInstanceOfType(service) ? service : null;
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new()
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
    }
}
