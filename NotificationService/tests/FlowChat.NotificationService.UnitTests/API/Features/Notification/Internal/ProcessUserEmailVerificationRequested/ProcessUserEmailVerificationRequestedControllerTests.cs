using AutoFixture;
using CSharpFunctionalExtensions;
using FlowChat.Core.Contracts;
using FlowChat.NotificationService.Api.Features.Notification.Internal.ProcessUserEmailVerificationRequested;
using FlowChat.NotificationService.Application.Features.Notification.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Infrastructure.Configuration;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.NotificationService.UnitTests;

public sealed class ProcessUserEmailVerificationRequestedControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Process_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController("expected-key", mediatorMock);

        var result = await controller.Process(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = _fixture.Create<Guid>(),
                Email = "john.doe@flowchat.local",
                UserName = "john.doe",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Process_WhenApiKeyMatches_SendsCommandAndReturnsAccepted()
    {
        UserEmailVerificationRequestedCommand? capturedCommand = null;

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<UserEmailVerificationRequestedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (UserEmailVerificationRequestedCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Process(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = _fixture.Create<Guid>(),
                Email = " john.doe@flowchat.local ",
                UserName = " john.doe ",
                DisplayName = " John Doe ",
                ConfirmationLink = " https://localhost/confirm ",
                SourceMessageKey = " source-key "
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Email.Should().Be("john.doe@flowchat.local");
        capturedCommand.UserName.Should().Be("john.doe");
        capturedCommand.DisplayName.Should().Be("John Doe");
        capturedCommand.ConfirmationLink.Should().Be("https://localhost/confirm");
        capturedCommand.SourceMessageKey.Should().Be("source-key");
    }

    [Fact]
    public async Task Process_WhenPayloadIsInvalid_ReturnsBadRequest()
    {
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Process(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = Guid.Empty,
                Email = "john.doe@flowchat.local",
                UserName = "john.doe",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static ProcessUserEmailVerificationRequestedController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<InternalApiSettingsSection>())
            .Returns(new InternalApiSettingsSection { ApiKey = expectedApiKey });

        var controller = new ProcessUserEmailVerificationRequestedController(
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
