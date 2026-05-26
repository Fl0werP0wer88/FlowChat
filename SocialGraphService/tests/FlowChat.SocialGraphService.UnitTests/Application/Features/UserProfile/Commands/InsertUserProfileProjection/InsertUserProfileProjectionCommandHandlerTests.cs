using AutoFixture;
using FlowChat.Core.Results;
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
    private readonly Mock<IDbUpdateExceptionClassifier> _dbUpdateExceptionClassifierMock = new();
    private readonly InsertUserProfileProjectionCommandHandler _handler;

    public InsertUserProfileProjectionCommandHandlerTests()
    {
        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserProfileProjectionDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<IdempotentCommandResult<Unit>>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        _domainEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new InsertUserProfileProjectionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _dbUpdateExceptionClassifierMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsNormalizedProjectionAndReturnsSuccess()
    {
        UserProfileProjectionDto? capturedProjection = null;
        _repositoryMock
            .Setup(x => x.InsertAsync(It.IsAny<UserProfileProjectionDto>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfileProjectionDto, CancellationToken>((projection, _) => capturedProjection = projection)
            .Returns(Task.CompletedTask);

        var command = new InsertUserProfileProjectionCommand(
            _fixture.Create<Guid>(),
            " jdoe ",
            " John ",
            " Doe ",
            " FlowChat ",
            " john@example.com ",
            true,
            true,
            " +48123123123 ",
            false,
            true,
            " https://example.com/avatar.jpg ",
            " hello there ",
            true,
            _fixture.Create<DateTimeOffset>());

        var result = await SendAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.WasAlreadyProcessed.Should().BeFalse();
        result.Value.Value.Should().Be(Unit.Value);
        capturedProjection.Should().NotBeNull();
        capturedProjection!.FriendlyUserId.Should().Be("jdoe");
        capturedProjection.MainEmail.Should().NotBeNull();
        capturedProjection.MainEmail!.Address.Should().Be("john@example.com");
        capturedProjection.MainEmail.IsConfirmed.Should().BeTrue();
        capturedProjection.MainEmail.IsVisible.Should().BeTrue();
        capturedProjection.MainPhone.Should().NotBeNull();
        capturedProjection.MainPhone!.Number.Should().Be("+48123123123");
        capturedProjection.MainPhone.IsConfirmed.Should().BeFalse();
        capturedProjection.MainPhone.IsVisible.Should().BeTrue();
        capturedProjection.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        capturedProjection.Bio.Should().Be("hello there");
        capturedProjection.FirstName.Should().Be("John");
        capturedProjection.LastName.Should().Be("Doe");
        capturedProjection.Organization.Should().Be("FlowChat");
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
            null,
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
            "Payload does not contain valid FriendlyUserId.");
        _repositoryMock.Verify(
            x => x.InsertAsync(It.IsAny<UserProfileProjectionDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private async Task<FlowChatResult<IdempotentCommandResult<Unit>>> SendAsync(InsertUserProfileProjectionCommand command)
    {
        var validator = new InsertUserProfileProjectionCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return FlowChatResult<IdempotentCommandResult<Unit>>.Failure(DomainError.Validation(errors: errors));
        }

        return await _handler.Handle(command, CancellationToken.None);
    }
}

