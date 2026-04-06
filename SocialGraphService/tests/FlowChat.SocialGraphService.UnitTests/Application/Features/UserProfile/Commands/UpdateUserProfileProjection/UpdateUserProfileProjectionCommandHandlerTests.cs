using AutoFixture;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UpdateUserProfileProjectionCommandHandlerTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IUserProfileProjectionWriteRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock = new();
    private readonly UpdateUserProfileProjectionCommandHandler _handler;

    public UpdateUserProfileProjectionCommandHandlerTests()
    {
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Unit>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Unit>>>, CancellationToken>((operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new UpdateUserProfileProjectionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_UpdatesNormalizedProjectionAndReturnsSuccess()
    {
        UserProfileProjection? capturedProjection = null;
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjection, CancellationToken>((projection, _) => capturedProjection = projection)
            .ReturnsAsync(true);

        var command = new UpdateUserProfileProjectionCommand(
            _fixture.Create<Guid>(),
            " jane.doe ",
            " Jane Doe ",
            " jane@example.com ",
            " +48987654321 ",
            " https://example.com/jane.jpg ",
            " updated bio ",
            false,
            _fixture.Create<DateTimeOffset>(),
            false,
            true,
            " Jane ",
            " Doe ",
            " FlowChat ");

        var result = await SendAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        capturedProjection.Should().NotBeNull();
        capturedProjection!.FriendlyUserId.Should().Be("jane.doe");
        capturedProjection.DisplayName.Should().Be("Jane Doe");
        capturedProjection.MainEmail.Should().Be("jane@example.com");
        capturedProjection.MainPhone.Should().Be("+48987654321");
        capturedProjection.AvatarUrl.Should().Be("https://example.com/jane.jpg");
        capturedProjection.Bio.Should().Be("updated bio");
        capturedProjection.FirstName.Should().Be("Jane");
        capturedProjection.LastName.Should().Be("Doe");
        capturedProjection.Organization.Should().Be("FlowChat");
    }

    [Fact]
    public async Task Handle_WhenProjectionDoesNotExist_ReturnsNotFound()
    {
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new UpdateUserProfileProjectionCommand(
            _fixture.Create<Guid>(),
            "jane.doe",
            "Jane Doe",
            null,
            null,
            null,
            null,
            true,
            null,
            true,
            false);

        var result = await SendAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be("User profile projection was not found.");
    }

    [Fact]
    public async Task Handle_WhenCommandIsInvalid_ReturnsValidationFailure()
    {
        var command = new UpdateUserProfileProjectionCommand(
            Guid.Empty,
            null,
            " ",
            null,
            null,
            null,
            null,
            true,
            null,
            true,
            false);

        var result = await SendAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);
        result.Error.Errors.Should().Equal(
            "Payload does not contain valid UserProfileId.",
            "Payload does not contain valid FriendlyUserId.",
            "Payload does not contain valid DisplayName.");
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserProfileProjection>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private async Task<FlowChatResult<Unit>> SendAsync(UpdateUserProfileProjectionCommand command)
    {
        var validator = new UpdateUserProfileProjectionCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return FlowChatResult<Unit>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }
}
