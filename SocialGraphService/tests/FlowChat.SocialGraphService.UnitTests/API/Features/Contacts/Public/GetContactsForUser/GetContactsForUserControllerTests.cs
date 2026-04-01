using AutoMapper;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Api.Features.Contacts.Public.GetContactsForUser;
using FlowChat.SocialGraphService.Application.Features.Contacts.Queries.GetContactsForUser;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class GetContactsForUserControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<GetContactsForUserMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

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

        var controller = SetupController(new GetContactsForUserController(_mediatorMock.Object, _mapper));

        var result = await controller.GetForUser(new GetContactsForUserRequest { UserId = userId }, CancellationToken.None);

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

        var controller = SetupController(new GetContactsForUserController(_mediatorMock.Object, _mapper));

        var result = await controller.GetForUser(new GetContactsForUserRequest { UserId = userId }, CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
        problemDetails.Detail.Should().Be($"Contacts for user '{userId}' were not found.");
    }

    private static TController SetupController<TController>(TController controller)
        where TController : ControllerBase
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
