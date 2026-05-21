using System.Security.Claims;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Api.Features.UserProfile.Public.AddEmail;
using FlowChat.UserProfileService.Api.Features.UserProfile.Public.AddPhone;
using FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfilesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private static TController SetupController<TController>(TController controller, Guid? authenticatedUserId = null)
        where TController : ControllerBase
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new SingleServiceProvider(new TestProblemDetailsFactory())
        };

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task AddEmail_ReturnsCreated_WithNewEmailId()
    {
        var userId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Success(
                new IdempotentCommandResult<Guid>(emailId, WasAlreadyProcessed: false)));

        var controller = SetupController(new AddEmailController(_mediatorMock.Object), userId);

        var result = await controller.AddEmail(new AddEmailRequest(Guid.NewGuid(), "john@example.com"), CancellationToken.None);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = created.Value.Should().BeOfType<AddEmailResponse>().Subject;
        response.EmailId.Should().Be(emailId);
    }

    [Fact]
    public async Task AddEmail_ReturnsOk_WhenAlreadyProcessed()
    {
        var userId = Guid.NewGuid();
        var emailId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Success(
                new IdempotentCommandResult<Guid>(emailId, WasAlreadyProcessed: true)));

        var controller = SetupController(new AddEmailController(_mediatorMock.Object), userId);

        var result = await controller.AddEmail(new AddEmailRequest(Guid.NewGuid(), "john@example.com"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AddEmailResponse>().Subject;
        response.EmailId.Should().Be(emailId);
    }

    [Fact]
    public async Task AddPhone_ReturnsCreated_WithNewPhoneId()
    {
        var userId = Guid.NewGuid();
        var phoneId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddPhoneCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Success(
                new IdempotentCommandResult<Guid>(phoneId, WasAlreadyProcessed: false)));

        var controller = SetupController(new AddPhoneController(_mediatorMock.Object), userId);

        var result = await controller.AddPhone(new AddPhoneRequest(Guid.NewGuid(), "+48123123123"), CancellationToken.None);

        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = created.Value.Should().BeOfType<AddPhoneResponse>().Subject;
        response.PhoneId.Should().Be(phoneId);
    }

    [Fact]
    public async Task AddPhone_ReturnsOk_WhenAlreadyProcessed()
    {
        var userId = Guid.NewGuid();
        var phoneId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddPhoneCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Success(
                new IdempotentCommandResult<Guid>(phoneId, WasAlreadyProcessed: true)));

        var controller = SetupController(new AddPhoneController(_mediatorMock.Object), userId);

        var result = await controller.AddPhone(new AddPhoneRequest(Guid.NewGuid(), "+48123123123"), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AddPhoneResponse>().Subject;
        response.PhoneId.Should().Be(phoneId);
    }

    [Fact]
    public async Task GetById_ReturnsProblemDetails_WhenProfileIsMissing()
    {
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<UserProfileDto>.Failure(
                DomainError.NotFound($"User profile '{userId}' was not found.")));

        var controller = SetupController(new UserProfilesController(_mediatorMock.Object), userId);

        var result = await controller.GetById(CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFound.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Be($"User profile '{userId}' was not found.");
    }

    [Fact]
    public async Task AddEmail_WhenCommandFails_ReturnsProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Failure(
                DomainError.Conflict("Email 'john@example.com' is already taken.")));

        var controller = SetupController(new AddEmailController(_mediatorMock.Object), Guid.NewGuid());

        var result = await controller.AddEmail(new AddEmailRequest(Guid.NewGuid(), "john@example.com"), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenProfileFound()
    {
        var userId = Guid.NewGuid();
        var dto = new UserProfileDto(userId, "jdoe", null, null, true, null, [], []);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<UserProfileDto>.Success(dto));

        var controller = SetupController(new UserProfilesController(_mediatorMock.Object), userId);

        var result = await controller.GetById(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<GetUserProfileResponse>().Subject;
        response.UserProfile.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_ReturnsUnauthorized_WhenNoClaimPresent()
    {
        var controller = SetupController(new UserProfilesController(_mediatorMock.Object));

        var result = await controller.GetById(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
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
