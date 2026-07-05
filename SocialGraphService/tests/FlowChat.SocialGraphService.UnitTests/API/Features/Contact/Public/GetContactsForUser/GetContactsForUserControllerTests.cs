using System.Security.Claims;
using AutoMapper;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Api.Features.Contact.Public.GetContactsForUser;
using FlowChat.SocialGraphService.Application.Features.Contact.Queries.GetContactsForUser;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class GetContactsForUserControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<GetContactsForUserMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task GetForUser_WhenQuerySucceeds_ReturnsOkResponse()
    {
        var userId = Guid.NewGuid();
        IReadOnlyList<ContactDto> contacts =
        [
            new(
                Guid.NewGuid(),
                userId,
                Guid.NewGuid(),
                "Jane Doe",
                "Jane",
                "Doe",
                "+48123123123",
                "jane@example.com",
                false)
        ];

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetContactsForUserQuery>(query => query.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyList<ContactDto>>.Success(contacts));

        var controller = SetupController(new GetContactsForUserController(_mediatorMock.Object, Mapper), userId);

        var result = await controller.GetForUser(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetContactsForUserResponse>().Subject;
        response.Contacts.Should().BeEquivalentTo(contacts);
    }

    [Fact]
    public async Task GetForUser_WhenQueryFails_ReturnsProblemDetails()
    {
        var userId = Guid.NewGuid();

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetContactsForUserQuery>(query => query.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyList<ContactDto>>.Failure(
                DomainError.NotFound($"Contacts for user '{userId}' were not found.")));

        var controller = SetupController(new GetContactsForUserController(_mediatorMock.Object, Mapper), userId);

        var result = await controller.GetForUser(CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Be($"Contacts for user '{userId}' were not found.");
    }

    [Fact]
    public async Task GetForUser_ReturnsUnauthorized_WhenNoClaimPresent()
    {
        var controller = SetupController(new GetContactsForUserController(_mediatorMock.Object, Mapper));

        var result = await controller.GetForUser(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

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
