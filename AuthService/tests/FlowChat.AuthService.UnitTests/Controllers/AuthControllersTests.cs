using AutoMapper;
using FlowChat.AuthService.API.Features.User.Public.LoginUser;
using FlowChat.AuthService.API.Features.User.Public.RefreshToken;
using FlowChat.AuthService.API.Features.User.Public.RegisterUser;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using FlowChat.Core.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.AuthService.UnitTests;

public sealed class AuthControllersTests
{
    [Fact]
    public async Task RegisterUserController_WhenMediatorReturnsSuccess_ReturnsCreated()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<RegisterUserCommandResponse>.Success(
                new RegisterUserCommandResponse { Id = Guid.NewGuid() }));

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

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
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
