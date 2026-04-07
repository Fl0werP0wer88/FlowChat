using AutoFixture;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Api.Features.Contact.Public.AddContact;
using FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class AddContactControllerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task Add_WhenCommandSucceeds_ReturnsCreatedResponse()
    {
        var ownerUserId = _fixture.Create<Guid>();
        var userId = _fixture.Create<Guid>();
        var contactId = _fixture.Create<Guid>();

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<AddContactCommand>(command =>
                    command.OwnerUserId == ownerUserId &&
                    command.UserId == userId &&
                    command.FriendlyUserId == null &&
                    command.Email == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Guid>.Success(contactId));

        var controller = SetupController(new AddContactController(_mediatorMock.Object));

        var result = await controller.Add(
            new AddContactRequest
            {
                OwnerUserId = ownerUserId,
                UserId = userId
            },
            CancellationToken.None);

        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<AddContactResponse>().Subject;
        response.ContactId.Should().Be(contactId);
    }

    [Fact]
    public async Task Add_WhenCommandFails_ReturnsProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AddContactCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Guid>.Failure(DomainError.Conflict("Contact already exists.")));

        var controller = SetupController(new AddContactController(_mediatorMock.Object));

        var result = await controller.Add(
            new AddContactRequest
            {
                OwnerUserId = _fixture.Create<Guid>(),
                FriendlyUserId = "jdoe"
            },
            CancellationToken.None);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var problemDetails = conflictResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Status.Should().Be(StatusCodes.Status409Conflict);
        problemDetails.Detail.Should().Be("Contact already exists.");
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
