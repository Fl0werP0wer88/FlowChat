using FlowChat.Core.Results;
using FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.PresenceService.UnitTests.API.Features.ContactObserverProjection.Internal.BulkUpsertOrDeleteUserContactProjection;

public sealed class BulkUpsertOrDeleteUserContactProjectionControllerTests
{
    [Fact]
    public async Task BulkUpsertOrDelete_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediatorMock);

        var result = await controller.BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserContactProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WhenApiKeyMatches_SendsCommandAndReturnsNoContent()
    {
        BulkUpsertOrDeleteUserContactProjectionCommand? capturedCommand = null;
        var upsertObservedUserId = Guid.NewGuid();
        var upsertObserverUserId = Guid.NewGuid();
        var deleteObservedUserId = Guid.NewGuid();
        var deleteObserverUserId = Guid.NewGuid();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>(
                (request, _) => capturedCommand = (BulkUpsertOrDeleteUserContactProjectionCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserContactProjectionRequest
            {
                Items =
                [
                    new BulkUpsertOrDeleteUserContactProjectionRequestItem
                    {
                        ObservedUserId = upsertObservedUserId,
                        ObserverUserId = upsertObserverUserId,
                        SourceVersion = 1,
                        Value = new BulkUpsertOrDeleteUserContactProjectionRequestValue
                        {
                            Source = "consumer"
                        }
                    },
                    new BulkUpsertOrDeleteUserContactProjectionRequestItem
                    {
                        ObservedUserId = deleteObservedUserId,
                        ObserverUserId = deleteObserverUserId,
                        SourceVersion = 2,
                        Value = null
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Items.Should().HaveCount(2);

        var upsertItem = capturedCommand.Items.Should().ContainSingle(x =>
            x.ObservedUserId == upsertObservedUserId &&
            x.ObserverUserId == upsertObserverUserId).Subject;
        upsertItem.SourceVersion.Should().Be(1);
        upsertItem.Value.Should().NotBeNull();
        upsertItem.Value!.Source.Should().Be("consumer");

        var deleteItem = capturedCommand.Items.Should().ContainSingle(x =>
            x.ObservedUserId == deleteObservedUserId &&
            x.ObserverUserId == deleteObserverUserId).Subject;
        deleteItem.SourceVersion.Should().Be(2);
        deleteItem.Value.Should().BeNull();
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WhenGuidIsEmpty_ReturnsBadRequestWithoutSendingCommand()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserContactProjectionRequest
            {
                Items =
                [
                    new BulkUpsertOrDeleteUserContactProjectionRequestItem
                    {
                        ObservedUserId = Guid.Empty,
                        ObserverUserId = Guid.NewGuid(),
                        SourceVersion = 1,
                        Value = new BulkUpsertOrDeleteUserContactProjectionRequestValue { Source = "consumer" }
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task BulkUpsertOrDelete_WhenPayloadInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<BulkUpsertOrDeleteUserContactProjectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.Validation(errors: ["Items must contain at least one item."])));
        var controller = CreateController(mediatorMock, "expected-key");

        var result = await controller.BulkUpsertOrDelete(
            new BulkUpsertOrDeleteUserContactProjectionRequest(),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static BulkUpsertOrDeleteUserContactProjectionController CreateController(
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new BulkUpsertOrDeleteUserContactProjectionController(
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
