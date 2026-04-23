using AutoMapper;
using FlowChat.AuthService.API.Features.User.Public.LoginUser;
using FlowChat.AuthService.API.Features.User.Public.RefreshToken;
using FlowChat.AuthService.API.Features.User.Public.RegisterUser;
using FlowChat.AuthService.Api.Features.User.Internal.ChangeAuthEmail;
using FlowChat.AuthService.Api.Features.User.Internal.ConfirmAuthEmail;
using FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.AuthService.UnitTests;

public sealed class AuthControllersTests
{
    [Fact]
    public async Task RegisterUserController_WhenMediatorReturnsSuccess_ReturnsOk()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<RegisterUserCommandResponse>.Success(new RegisterUserCommandResponse { Id = Guid.NewGuid() }));

        var controller = CreateController(new RegisterUserController(mediatorMock.Object, CreateMapper()));

        var result = await controller.Create(
            new RegisterUserRequest
            {
                Id = Guid.NewGuid(),
                FriendlyUserId = "flower",
                Email = "flower@example.com",
                Password = "P@ssw0rd!",
                FirstName = "Flower",
                LastName = "Power",
                Organization = "FlowChat"
            },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task LoginUserController_WhenOpenIddictRequestIsMissing_ReturnsBadRequest()
    {
        var controller = CreateController(new LoginUserController(Mock.Of<IMediator>()));

        var result = await controller.Login(CancellationToken.None);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task RefreshTokenController_WhenOpenIddictRequestIsMissing_ReturnsBadRequest()
    {
        var controller = CreateController(new RefreshTokenController(Mock.Of<IMediator>()));

        var result = await controller.RefreshToken(CancellationToken.None);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task ChangeAuthEmailController_WithMissingApiKey_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>();
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<InternalApiSettingsSection>())
            .Returns(new InternalApiSettingsSection { ApiKey = "expected-key" });

        var controller = CreateController(
            new ChangeAuthEmailController(mediatorMock.Object, settingsProviderMock.Object));

        var result = await controller.ChangeAuthEmail(
            new ChangeAuthEmailRequest
            {
                UserId = Guid.NewGuid(),
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task ConfirmAuthEmailController_WithMissingApiKey_ReturnsUnauthorized()
    {
        var mediatorMock = new Mock<IMediator>();
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<InternalApiSettingsSection>())
            .Returns(new InternalApiSettingsSection { ApiKey = "expected-key" });

        var controller = CreateController(
            new ConfirmAuthEmailController(mediatorMock.Object, settingsProviderMock.Object));

        var result = await controller.ConfirmEmail(
            new ConfirmAuthEmailRequest { EmailAddress = "flower@example.com" },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task ChangeAuthEmailController_WithValidApiKey_MapsRequestToCommand()
    {
        var mediatorMock = new Mock<IMediator>();
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<InternalApiSettingsSection>())
            .Returns(new InternalApiSettingsSection { ApiKey = "expected-key" });

        ChangeAuthEmailCommand? capturedCommand = null;
        mediatorMock
            .Setup(x => x.Send(It.IsAny<ChangeAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (ChangeAuthEmailCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController(
            new ChangeAuthEmailController(mediatorMock.Object, settingsProviderMock.Object));
        controller.Request.Headers["X-Internal-Api-Key"] = "expected-key";

        var userId = Guid.NewGuid();
        var result = await controller.ChangeAuthEmail(
            new ChangeAuthEmailRequest
            {
                UserId = userId,
                EmailAddress = "flower@example.com"
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.EmailAddress.Should().Be("flower@example.com");
    }

    private static TController CreateController<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(RegisterUserController).Assembly),
            NullLoggerFactory.Instance).CreateMapper();
    }
}
