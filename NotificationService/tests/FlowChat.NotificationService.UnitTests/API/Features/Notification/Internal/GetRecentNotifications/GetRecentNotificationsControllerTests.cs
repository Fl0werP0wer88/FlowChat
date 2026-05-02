using AutoFixture;
using CSharpFunctionalExtensions;
using FlowChat.Core.Contracts;
using FlowChat.NotificationService.Api.Features.Notification.Internal.GetRecentNotifications;
using FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;
using FlowChat.NotificationService.Domain.Enums;
using FlowChat.NotificationService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.NotificationService.UnitTests.API.Features.Notification.Internal.GetRecentNotifications;

public sealed class GetRecentNotificationsControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Get_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController("expected-key", mediatorMock);

        var result = await controller.Get(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Get_WhenApiKeyMatches_SendsRecentNotificationsQueryAndReturnsOk()
    {
        GetNotificationsQuery? capturedQuery = null;
        var userId = _fixture.Create<Guid>();
        var dtos = new List<NotificationDto>
        {
            new(
                Guid.NewGuid(),
                userId,
                "john.doe@flowchat.local",
                "John Doe",
                "Please confirm your email",
                NotificationType.EmailVerification,
                NotificationStatus.Sent,
                "message-1",
                null,
                "key-1",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)
        };

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<GetNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<IReadOnlyList<NotificationDto>>>, CancellationToken>(
                (request, _) => capturedQuery = (GetNotificationsQuery)request)
            .ReturnsAsync(FlowChatResult<IReadOnlyList<NotificationDto>>.Success(dtos));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Get(CancellationToken.None);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.UserId.Should().BeNull();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetRecentNotificationsResponse>().Subject;
        response.Notifications.Should().BeEquivalentTo(dtos);
    }

    private static GetRecentNotificationsController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<InternalApiSettingsSection>())
            .Returns(new InternalApiSettingsSection { ApiKey = expectedApiKey });

        var controller = new GetRecentNotificationsController(
            mediatorMock.Object,
            settingsProviderMock.Object);

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
