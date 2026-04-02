using AutoFixture;
using AutoMapper;
using FlowChat.AuthService.API.Features.User.Public.LoginUser;
using FlowChat.AuthService.API.Features.User.Public.RegisterUser;
using FlowChat.AuthService.Api.Features.User.Internal.ConfirmAuthEmail;
using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Application.Features.User.Commands.LoginUser;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests.Controllers;

public sealed class AuthControllersTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Register_WhenRegistrationFailsWithConflict_ReturnsConflictProblemDetails()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<RegisterUserCommandResponse>.Failure(
                DomainError.Conflict("User with the provided username or email already exists.")));

        var controller = CreateController(new RegisterUserController(mediatorMock.Object, CreateMapper()));

        var result = await controller.Create(
            new RegisterUserRequest
            {
                UserName = "jdoe",
                Email = "john@example.com",
                Password = "Password123!"
            },
            CancellationToken.None);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var problemDetails = conflict.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status409Conflict);
        problemDetails.Detail.Should().Be("User with the provided username or email already exists.");
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorizedProblemDetails()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<LoginUserCommandResponse>.Failure(
                DomainError.Unauthorized("Invalid credentials or account is not confirmed.")));

        var controller = CreateController(new LoginUserController(mediatorMock.Object, CreateMapper()));

        var result = await controller.Login(
            new LoginUserRequest
            {
                Login = "jdoe",
                Password = "Password123!"
            },
            CancellationToken.None);

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problemDetails = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problemDetails.Detail.Should().Be("Invalid credentials or account is not confirmed.");
    }

    [Fact]
    public async Task ConfirmAuthEmail_WhenInternalApiKeyIsInvalid_ReturnsUnauthorized()
    {
        var expectedApiKey = _fixture.Create<string>();
        var mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        var apiSettingsManagerMock = new Mock<IApiSettingsManager>();
        apiSettingsManagerMock
            .Setup(x => x.GetInternalApiSettings())
            .Returns(new InternalApiSettings { ApiKey = expectedApiKey });

        var controller = CreateController(
            new ConfirmAuthEmailController(mediatorMock.Object, apiSettingsManagerMock.Object),
            new Dictionary<string, string?>
            {
                ["X-Internal-Api-Key"] = $"{expectedApiKey}-invalid"
            });

        var result = await controller.ConfirmEmail(
            new ConfirmAuthEmailRequest
            {
                EmailAddress = "john@example.com"
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        mediatorMock.Verify(
            x => x.Send(It.IsAny<ConfirmAuthEmailCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmAuthEmail_WhenInternalApiKeyIsValid_SendsCommand()
    {
        var expectedApiKey = _fixture.Create<string>();
        ConfirmAuthEmailCommand? capturedCommand = null;

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<ConfirmAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (ConfirmAuthEmailCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var apiSettingsManagerMock = new Mock<IApiSettingsManager>();
        apiSettingsManagerMock
            .Setup(x => x.GetInternalApiSettings())
            .Returns(new InternalApiSettings { ApiKey = expectedApiKey });

        var controller = CreateController(
            new ConfirmAuthEmailController(mediatorMock.Object, apiSettingsManagerMock.Object),
            new Dictionary<string, string?>
            {
                ["X-Internal-Api-Key"] = expectedApiKey
            });

        var result = await controller.ConfirmEmail(
            new ConfirmAuthEmailRequest
            {
                EmailAddress = "john@example.com"
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.EmailAddress.Should().Be("john@example.com");
    }

    private static TController CreateController<TController>(
        TController controller,
        IDictionary<string, string?>? headers = null)
        where TController : ControllerBase
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                if (header.Value is not null)
                {
                    httpContext.Request.Headers[header.Key] = header.Value;
                }
            }
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(RegisterUserController).Assembly),
            NullLoggerFactory.Instance);

        return configuration.CreateMapper();
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
            string? instance = null)
        {
            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            return new ValidationProblemDetails(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
        }
    }
}
