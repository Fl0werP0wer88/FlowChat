using System.Security.Claims;
using AutoFixture;
using FlowChat.NotificationService.Api.Features.Notification.Public.GetNotifications;
using FlowChat.NotificationService.Application.Features.Notification.Queries.GetNotifications;
using FlowChat.NotificationService.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.NotificationService.UnitTests.API.Features.Notification.Public.GetNotifications;

public sealed class NotificationsControllerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();

    private NotificationsController CreateController(Guid? authenticatedUserId = null)
    {
        var controller = new NotificationsController(_mediatorMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task Get_WhenQuerySucceeds_ReturnsOkWithNotifications()
    {
        var userId = _fixture.Create<Guid>();
        var dtos = new List<NotificationDto>
        {
            new(Guid.NewGuid(), userId, "a@b.com", "Alice", "Please confirm your email", NotificationType.EmailVerification,
                NotificationStatus.Sent, "msg-1", null, "key-1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChat.Core.Results.FlowChatResult<IReadOnlyList<NotificationDto>>.Success(dtos));

        var controller = CreateController(userId);
        var result = await controller.Get(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetNotificationsResponse>().Subject;
        response.Notifications.Should().BeEquivalentTo(dtos);
    }

    [Fact]
    public async Task Get_PassesUserIdFromClaimToQuery()
    {
        var userId = _fixture.Create<Guid>();
        GetNotificationsQuery? capturedQuery = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChat.Core.Results.FlowChatResult<IReadOnlyList<NotificationDto>>>, CancellationToken>(
                (q, _) => capturedQuery = (GetNotificationsQuery)q)
            .ReturnsAsync(FlowChat.Core.Results.FlowChatResult<IReadOnlyList<NotificationDto>>.Success(
                new List<NotificationDto>()));

        var controller = CreateController(userId);
        await controller.Get(CancellationToken.None);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Get_WhenQueryReturnsEmptyList_ReturnsOkWithEmptyNotifications()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChat.Core.Results.FlowChatResult<IReadOnlyList<NotificationDto>>.Success(
                new List<NotificationDto>()));

        var controller = CreateController(_fixture.Create<Guid>());
        var result = await controller.Get(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetNotificationsResponse>().Subject;
        response.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ReturnsUnauthorized_WhenNoClaimPresent()
    {
        var controller = CreateController();

        var result = await controller.Get(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // --- Test helpers ---

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
