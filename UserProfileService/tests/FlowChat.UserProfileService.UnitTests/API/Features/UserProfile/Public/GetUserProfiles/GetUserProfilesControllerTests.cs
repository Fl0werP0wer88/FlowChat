using AutoFixture;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfiles;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfiles;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class GetUserProfilesControllerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task GetByIds_WhenQuerySucceeds_ReturnsOkResponse()
    {
        var firstUserId = _fixture.Create<Guid>();
        var secondUserId = _fixture.Create<Guid>();
        IReadOnlyList<UserProfileDto> userProfiles =
        [
            new UserProfileDto(firstUserId, "jdoe", null, null, null, null, null, true, null, [], []),
            new UserProfileDto(secondUserId, "asmith", null, null, null, null, null, true, null, [], [])
        ];

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetUserProfilesQuery>(query => query.UserIds.SequenceEqual(new[] { firstUserId, secondUserId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyList<UserProfileDto>>.Success(userProfiles));

        var controller = SetupController(new GetUserProfilesController(_mediatorMock.Object));

        var result = await controller.GetByIds(
            new GetUserProfilesRequest
            {
                UserIds = [firstUserId, secondUserId]
            },
            CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetUserProfilesResponse>().Subject;
        response.UserProfiles.Should().BeEquivalentTo(userProfiles);
    }

    [Fact]
    public async Task GetByIds_WhenQueryFails_ReturnsBadRequestProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetUserProfilesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyList<UserProfileDto>>.Failure(
                DomainError.Validation(errors: ["Query must contain at least one user ID."])));

        var controller = SetupController(new GetUserProfilesController(_mediatorMock.Object));

        var result = await controller.GetByIds(new GetUserProfilesRequest(), CancellationToken.None);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
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
