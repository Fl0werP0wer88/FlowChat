using AutoFixture;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class InsertUserProfileProjectionCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileProjectionWriteRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly InsertUserProfileProjectionCommandHandler _handler;

    public InsertUserProfileProjectionCommandHandlerTests()
    {
        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new InsertUserProfileProjectionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsNormalizedProjectionAndReturnsSuccess()
    {
        UserProfileProjection? capturedProjection = null;
        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjection, CancellationToken>((projection, _) => capturedProjection = projection)
            .ReturnsAsync(true);

        var command = new InsertUserProfileProjectionCommand(
            _fixture.Create<Guid>(),
            " jdoe ",
            " John Doe ",
            " john@example.com ",
            " +48123123123 ",
            " https://example.com/avatar.jpg ",
            " hello there ",
            true,
            _fixture.Create<DateTimeOffset>(),
            " John ",
            " Doe ",
            " FlowChat ");

        var result = await SendAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        capturedProjection.Should().NotBeNull();
        capturedProjection!.FriendlyUserId.Should().Be("jdoe");
        capturedProjection.DisplayName.Should().Be("John Doe");
        capturedProjection.MainEmail.Should().Be("john@example.com");
        capturedProjection.MainPhone.Should().Be("+48123123123");
        capturedProjection.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        capturedProjection.Bio.Should().Be("hello there");
        capturedProjection.FirstName.Should().Be("John");
        capturedProjection.LastName.Should().Be("Doe");
        capturedProjection.Organization.Should().Be("FlowChat");
    }

    [Fact]
    public async Task Handle_WhenProjectionAlreadyExists_ReturnsConflict()
    {
        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new InsertUserProfileProjectionCommand(
            _fixture.Create<Guid>(),
            "jdoe",
            "John Doe",
            null,
            null,
            null,
            null,
            true,
            null);

        var result = await SendAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.ErrorMessage.Should().Be("User profile projection already exists.");
    }

    [Fact]
    public async Task Handle_WhenCommandIsInvalid_ReturnsValidationFailure()
    {
        var command = new InsertUserProfileProjectionCommand(
            Guid.Empty,
            " ",
            null,
            null,
            null,
            null,
            null,
            true,
            null);

        var result = await SendAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal(
            "Payload does not contain valid UserProfileId.",
            "Payload does not contain valid FriendlyUserId.",
            "Payload does not contain valid DisplayName.");
        _repositoryMock.Verify(
            x => x.InsertAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private async Task<FlowChatResult<Unit>> SendAsync(InsertUserProfileProjectionCommand command)
    {
        var validator = new InsertUserProfileProjectionCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return FlowChatResult<Unit>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }
}
