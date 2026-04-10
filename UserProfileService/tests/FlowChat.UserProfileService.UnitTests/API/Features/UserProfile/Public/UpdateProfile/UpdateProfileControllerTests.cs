using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Api.Features.UserProfile.Public.UpdateProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UpdateProfileControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private static UpdateProfileController SetupController(UpdateProfileController controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
            }
        };
        return controller;
    }

    [Fact]
    public async Task UpdateProfile_WhenCommandSucceeds_ReturnsNoContent()
    {
        UpdateProfileCommand? capturedCommand = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateProfileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Guid>>, CancellationToken>((request, _) => capturedCommand = request as UpdateProfileCommand)
            .ReturnsAsync(FlowChatResult<Guid>.Success(Guid.NewGuid()));

        var controller = SetupController(new UpdateProfileController(_mediatorMock.Object));
        var userId = Guid.NewGuid();

        var result = await controller.UpdateProfile(
            userId,
            new UpdateProfileRequest("John", "Doe", "FlowChat", "https://cdn.example/avatar.png", "about me", false),
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.FirstName.Should().Be("John");
        capturedCommand.LastName.Should().Be("Doe");
        capturedCommand.Organization.Should().Be("FlowChat");
        capturedCommand.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
        capturedCommand.Bio.Should().Be("about me");
        capturedCommand.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProfile_WhenCommandFails_ReturnsProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Guid>.Failure(DomainError.NotFound("User profile was not found.")));

        var controller = SetupController(new UpdateProfileController(_mediatorMock.Object));

        var result = await controller.UpdateProfile(
            Guid.NewGuid(),
            new UpdateProfileRequest("John", "Doe", "FlowChat", null, null, true),
            CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
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
