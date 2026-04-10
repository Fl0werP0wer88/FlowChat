using AutoFixture;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Public.SearchUserProfileProjections;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class SearchUserProfileProjectionsControllerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public void Controller_RouteTemplate_UsesSocialGraphPath()
    {
        var routeAttribute = typeof(SearchUserProfileProjectionsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        routeAttribute.Template.Should().Be("api/userprofiles/projections/socialgraph");
    }

    [Fact]
    public async Task Search_WhenQuerySucceeds_ReturnsOkResponse()
    {
        IReadOnlyList<UserProfileProjectionDto> projections =
        [
            new UserProfileProjectionDto
            {
                UserProfileId = _fixture.Create<Guid>(),
                FriendlyUserId = "jdoe",
                FirstName = "Jane",
                LastName = "Doe",
                Organization = "FlowChat",
                MainEmail = new UserProfileProjectionEmailDto
                {
                    Address = "jane@example.com",
                    IsConfirmed = true,
                    IsVisible = true
                },
                MainPhone = new UserProfileProjectionPhoneDto
                {
                    Number = "+48123123123",
                    IsConfirmed = true,
                    IsVisible = true
                },
                IsActive = true,
                LastSeenAtUtc = _fixture.Create<DateTimeOffset>()
            }
        ];

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<SearchUserProfileProjectionsQuery>(query =>
                    query.FirstName == "Jan" &&
                    query.LastName == null &&
                    query.Organization == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyList<UserProfileProjectionDto>>.Success(projections));

        var controller = SetupController(new SearchUserProfileProjectionsController(_mediatorMock.Object));

        var result = await controller.Search(
            new SearchUserProfileProjectionsRequest
            {
                FirstName = "Jan"
            },
            CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SearchUserProfileProjectionsResponse>().Subject;
        response.UserProfiles.Should().BeEquivalentTo(projections);
    }

    [Fact]
    public async Task Search_WhenQueryFails_ReturnsBadRequestProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<SearchUserProfileProjectionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyList<UserProfileProjectionDto>>.Failure(
                DomainError.Validation(errors: ["Query must contain at least one search criterion."])));

        var controller = SetupController(new SearchUserProfileProjectionsController(_mediatorMock.Object));

        var result = await controller.Search(new SearchUserProfileProjectionsRequest(), CancellationToken.None);

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

