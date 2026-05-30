using FlowChat.Shared.Domain;
using FlowChat.Core.Results;
using FlowChat.UserProfileService.Api.Features.UserProfile.Internal.CreateInitialUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class CreateInitialUserProfileControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public CreateInitialUserProfileControllerTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateInitialUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Guid>.Success(Guid.NewGuid()));
    }

    private CreateInitialUserProfileController CreateController(string apiKey) =>
        new(_mediatorMock.Object, Options.Create(new InternalApiSettingsSection { ApiKey = apiKey }));

    private static void SetupHttpContext(CreateInitialUserProfileController controller, string? apiKeyHeader = null)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };
        if (apiKeyHeader is not null)
        {
            httpContext.Request.Headers["X-Internal-Api-Key"] = apiKeyHeader;
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task CreateInitialUserProfile_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key");
        SetupHttpContext(controller); // no header

        var result = await controller.CreateInitialUserProfile(
            new CreateInitialUserProfileRequest
            {
                UserId = Guid.NewGuid(),
                FriendlyUserId = "jdoe",
                FirstName = "John",
                LastName = "Doe"
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateInitialUserProfile_WhenApiKeyMatches_DispatchesCommand()
    {
        var controller = CreateController("expected-key");
        SetupHttpContext(controller, "expected-key");
        CreateInitialUserProfileCommand? capturedCommand = null;
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<IRequest<FlowChatResult<Guid>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Guid>>, CancellationToken>(
                (request, _) => capturedCommand = request as CreateInitialUserProfileCommand)
            .ReturnsAsync(FlowChatResult<Guid>.Success(Guid.NewGuid()));

        var userId = Guid.NewGuid();

        var result = await controller.CreateInitialUserProfile(
            new CreateInitialUserProfileRequest
            {
                UserId = userId,
                FriendlyUserId = "jdoe",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            CancellationToken.None);

        result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.FriendlyUserId.Should().Be("jdoe");
        capturedCommand.FirstName.Should().Be("John");
        capturedCommand.LastName.Should().Be("Doe");
        capturedCommand.Email.Should().Be("john@example.com");
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
