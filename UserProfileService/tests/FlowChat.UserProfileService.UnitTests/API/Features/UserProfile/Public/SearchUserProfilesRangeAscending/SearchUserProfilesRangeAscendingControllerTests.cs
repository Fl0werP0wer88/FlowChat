using AutoMapper;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfilesRangeAscending;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class SearchUserProfilesRangeAscendingControllerTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<SearchUserProfilesRangeAscendingMappingProfile>(),
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public void Controller_EndpointMetadata_UsesExpectedAuthorizedRoute()
    {
        var route = typeof(SearchUserProfilesRangeAscendingController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();
        var authorize = typeof(SearchUserProfilesRangeAscendingController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);

        route.Template.Should().Be("api/userprofiles/search/range/ascending");
        authorize.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchRangeAscending_WhenQuerySucceeds_ReturnsMappedLightweightPage()
    {
        var item = new UserProfileSearchResultDto(
            Guid.NewGuid(),
            "jdoe",
            "Jane",
            "Doe",
            "FlowChat",
            "https://cdn.example/avatar.png");
        var page = new SearchUserProfilesRangeAscendingPageDto([item], "jdoe", true);
        _mediatorMock
            .Setup(mediator => mediator.Send(
                It.Is<SearchUserProfilesRangeAscendingQuery>(query =>
                    query.FirstName == "Jan" &&
                    query.LastName == "Do" &&
                    query.Organization == "Flow" &&
                    query.Cursor == "alice" &&
                    query.Limit == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<SearchUserProfilesRangeAscendingPageDto>.Success(page));
        var controller = SetupController(
            new SearchUserProfilesRangeAscendingController(_mediatorMock.Object, Mapper));

        var result = await controller.SearchRangeAscending(
            new SearchUserProfilesRangeAscendingRequest
            {
                FirstName = "Jan",
                LastName = "Do",
                Organization = "Flow",
                Cursor = "alice",
                Limit = 10
            },
            CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value
            .Should().BeOfType<SearchUserProfilesRangeAscendingResponse>().Subject;
        response.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(item);
        response.NextCursor.Should().Be("jdoe");
        response.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task SearchRangeAscending_WhenQueryFails_ReturnsBadRequestProblemDetails()
    {
        _mediatorMock
            .Setup(mediator => mediator.Send(
                It.IsAny<SearchUserProfilesRangeAscendingQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<SearchUserProfilesRangeAscendingPageDto>.Failure(
                DomainError.Validation(errors: ["Query Cursor must be a valid FriendlyUserId."])));
        var controller = SetupController(
            new SearchUserProfilesRangeAscendingController(_mediatorMock.Object, Mapper));

        var result = await controller.SearchRangeAscending(
            new SearchUserProfilesRangeAscendingRequest
            {
                FirstName = "Jane",
                Cursor = "invalid cursor"
            },
            CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ProblemDetails>()
            .Which.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Request_WhenLimitIsNotProvided_UsesDefaultLimit()
    {
        var request = new SearchUserProfilesRangeAscendingRequest();

        request.Limit.Should().Be(SearchUserProfilesRangeAscendingController.DefaultLimit);
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
