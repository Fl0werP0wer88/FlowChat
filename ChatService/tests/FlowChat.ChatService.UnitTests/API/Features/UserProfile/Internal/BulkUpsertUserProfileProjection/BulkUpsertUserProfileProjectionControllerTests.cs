using FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;
using FlowChat.ChatService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;

public sealed class BulkUpsertUserProfileProjectionControllerTests
{
    [Fact]
    public async Task BulkUpsert_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediatorMock);

        var result = await controller.BulkUpsert(
            new BulkUpsertUserProfileProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task BulkUpsert_WhenApiKeyMatches_SendsCommandAndReturnsOkResponse()
    {
        BulkUpsertUserProfileProjectionCommand? capturedCommand = null;
        var userProfileId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<BulkUpsertCommandResult>>, CancellationToken>(
                (request, _) => capturedCommand = (BulkUpsertUserProfileProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(new BulkUpsertCommandResult(1, 1)));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsert(
            new BulkUpsertUserProfileProjectionRequest
            {
                Items =
                [
                    new UserProfileProjectionRequest
                    {
                        UserProfileId = userProfileId,
                        FriendlyUserId = " user-1 ",
                        DisplayName = " User One ",
                        AvatarUrl = " https://example.com/avatar.png ",
                        CreatedBy = "source",
                        CreatedAtUtc = now,
                        LastModifiedBy = "source",
                        LastModifiedAtUtc = now
                    }
                ]
            },
            CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BulkUpsertUserProfileProjectionResponse>().Subject;
        response.RequestedCount.Should().Be(1);
        response.UpsertedCount.Should().Be(1);
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().ContainSingle().Which.Should().Be(
            new BulkUpsertUserProfileProjectionCommandItem(
                userProfileId,
                " user-1 ",
                " User One ",
                " https://example.com/avatar.png ",
                "source",
                now,
                "source",
                now));
    }

    [Fact]
    public async Task BulkUpsert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertUserProfileProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Failure(
                DomainError.Validation(errors: ["Items must contain at least one item."])));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsert(
            new BulkUpsertUserProfileProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static BulkUpsertUserProfileProjectionController CreateController(
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new BulkUpsertUserProfileProjectionController(
            mediatorMock.Object,
            Options.Create(new InternalApiSettingsSection { ApiKey = "expected-key" }));

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        if (providedApiKey is not null)
        {
            httpContext.Request.Headers["X-Internal-Api-Key"] = providedApiKey;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
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
