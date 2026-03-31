using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Api.Features.UserProfiles.Internal.CreateInitialUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

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

    private CreateInitialUserProfileController CreateController(string apiKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = apiKey
            })
            .Build();

        return new CreateInitialUserProfileController(_mediatorMock.Object, new ApiSettingsManager(configuration));
    }

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
                UserName = "jdoe",
                DisplayName = "John Doe"
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
            .Setup(x => x.Send(It.IsAny<IRequest<FlowChatResult<Guid>>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Guid>>, CancellationToken>((request, _) => capturedCommand = request as CreateInitialUserProfileCommand)
            .ReturnsAsync(FlowChatResult<Guid>.Success(Guid.NewGuid()));

        var userId = Guid.NewGuid();

        var result = await controller.CreateInitialUserProfile(
            new CreateInitialUserProfileRequest
            {
                UserId = userId,
                UserName = "jdoe",
                DisplayName = "John Doe",
                Email = "john@example.com"
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.UserName.Should().Be("jdoe");
        capturedCommand.DisplayName.Should().Be("John Doe");
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
