using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;
using FlowChat.UserProfileService.Consumers.Kafka;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AccountRegisteredSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<AccountRegisteredSubscriber>> _loggerMock = new();
    private readonly AccountRegisteredSubscriber _subscriber;

    public AccountRegisteredSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateInitialUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Guid>.Success(Guid.NewGuid()));

        _subscriber = new AccountRegisteredSubscriber(
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenPayloadIsValid_MapsRequestToCommand()
    {
        var userId = _fixture.Create<Guid>();
        CreateInitialUserProfileCommand? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateInitialUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Guid>>, CancellationToken>((request, _) =>
                capturedCommand = (CreateInitialUserProfileCommand)request)
            .ReturnsAsync(FlowChatResult<Guid>.Success(Guid.NewGuid()));

        var message = new AccountRegisteredIntegrationEvent
        {
            UserId = userId,
            FriendlyUserId = " jdoe ",
            Email = "john@example.com",
            FirstName = " John ",
            LastName = " Doe ",
            Organization = " FlowChat "
        };

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.FriendlyUserId.Should().Be("jdoe");
        capturedCommand.Email.Should().Be("john@example.com");
        capturedCommand.FirstName.Should().Be("John");
        capturedCommand.LastName.Should().Be("Doe");
        capturedCommand.Organization.Should().Be("FlowChat");
    }

    [Fact]
    public async Task HandleAsync_WhenFriendlyUserIdIsMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("FriendlyUserId is required.");

        var message = new AccountRegisteredIntegrationEvent
        {
            UserId = _fixture.Create<Guid>(),
            FriendlyUserId = "   ",
            Email = "test@example.com"
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*FriendlyUserId*");

        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("UserId is required.");

        var message = new AccountRegisteredIntegrationEvent
        {
            UserId = Guid.Empty,
            FriendlyUserId = "jdoe",
            Email = "test@example.com"
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*UserId*");

        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenOptionalFieldsAreBlank_MapsNullOptionalFields()
    {
        CreateInitialUserProfileCommand? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateInitialUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Guid>>, CancellationToken>((request, _) =>
                capturedCommand = (CreateInitialUserProfileCommand)request)
            .ReturnsAsync(FlowChatResult<Guid>.Success(Guid.NewGuid()));

        var message = new AccountRegisteredIntegrationEvent
        {
            UserId = _fixture.Create<Guid>(),
            FriendlyUserId = "jdoe",
            Email = "john@example.com",
            FirstName = " ",
            LastName = null,
            Organization = "\t"
        };

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.FirstName.Should().BeNull();
        capturedCommand.LastName.Should().BeNull();
        capturedCommand.Organization.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateInitialUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Guid>.Failure(DomainError.BadRequest("boom")));

        var message = new AccountRegisteredIntegrationEvent
        {
            UserId = _fixture.Create<Guid>(),
            FriendlyUserId = "jdoe",
            Email = "john@example.com"
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");
    }

    private void SetupCommandFailure(string errorMessage)
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateInitialUserProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Guid>.Failure(DomainError.BadRequest(errorMessage)));
    }

    private void VerifyCommandWasSent()
    {
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<CreateInitialUserProfileCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
