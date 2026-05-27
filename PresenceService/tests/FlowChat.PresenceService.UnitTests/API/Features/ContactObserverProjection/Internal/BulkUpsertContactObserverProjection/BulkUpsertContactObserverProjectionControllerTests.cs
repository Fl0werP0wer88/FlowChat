using FlowChat.Core.Results;
using FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal;
using FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.BulkUpsertContactObserverProjection;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertContactObserverProjection;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
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

namespace FlowChat.PresenceService.UnitTests.API.Features.ContactObserverProjection.Internal.BulkUpsertContactObserverProjection;

public sealed class BulkUpsertContactObserverProjectionControllerTests
{
    [Fact]
    public async Task BulkUpsert_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediatorMock);

        var result = await controller.BulkUpsert(
            new BulkUpsertContactObserverProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task BulkUpsert_WhenApiKeyMatches_SendsCommandAndReturnsOkResponse()
    {
        BulkUpsertContactObserverProjectionCommand? capturedCommand = null;
        var observedUserId = Guid.NewGuid();
        var observerUserId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertContactObserverProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<BulkUpsertCommandResult>>, CancellationToken>(
                (request, _) => capturedCommand = (BulkUpsertContactObserverProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Success(new BulkUpsertCommandResult(1, 1)));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsert(
            new BulkUpsertContactObserverProjectionRequest
            {
                Items =
                [
                    new ContactObserverProjectionRequest
                    {
                        ObservedUserId = observedUserId,
                        ObserverUserId = observerUserId
                    }
                ]
            },
            CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BulkUpsertContactObserverProjectionResponse>().Subject;
        response.RequestedCount.Should().Be(1);
        response.UpsertedCount.Should().Be(1);
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().ContainSingle().Which.Should().Be(
            new BulkUpsertContactObserverProjectionCommandItem(observedUserId, observerUserId));
    }

    [Fact]
    public async Task BulkUpsert_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertContactObserverProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<BulkUpsertCommandResult>.Failure(
                DomainError.Validation(errors: ["Items must contain at least one item."])));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsert(
            new BulkUpsertContactObserverProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static BulkUpsertContactObserverProjectionController CreateController(
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new BulkUpsertContactObserverProjectionController(
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
