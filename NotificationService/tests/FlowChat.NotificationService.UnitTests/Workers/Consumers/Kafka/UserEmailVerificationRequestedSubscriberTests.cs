using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Results;
using FlowChat.NotificationService.Application.Features.Notification.Commands.UserEmailVerificationRequested;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.NotificationService.UnitTests;

public sealed class UserEmailVerificationRequestedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<UserEmailVerificationRequestedSubscriber>> _loggerMock = new();
    private readonly UserEmailVerificationRequestedSubscriber _subscriber;

    public UserEmailVerificationRequestedSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UserEmailVerificationRequestedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new UserEmailVerificationRequestedSubscriber(
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenEventArrives_SendsNormalizedCommand()
    {
        UserEmailVerificationRequestedCommand? capturedCommand = null;
        var userId = _fixture.Create<Guid>();
        var verificationRequestId = _fixture.Create<Guid>();

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UserEmailVerificationRequestedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (UserEmailVerificationRequestedCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                VerificationRequestId = verificationRequestId,
                UserId = userId,
                UserEmail = " john.doe@flowchat.local ",
                ConfirmationLink = " https://localhost/confirm "
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.Email.Should().Be("john.doe@flowchat.local");
        capturedCommand.UserName.Should().Be("john.doe");
        capturedCommand.DisplayName.Should().Be("john.doe");
        capturedCommand.ConfirmationLink.Should().Be("https://localhost/confirm");
        capturedCommand.SourceMessageKey.Should().Be(verificationRequestId.ToString("D"));
    }

    [Fact]
    public async Task HandleAsync_WhenVerificationRequestIdIsEmpty_UsesUserIdAsSourceMessageKey()
    {
        UserEmailVerificationRequestedCommand? capturedCommand = null;
        var userId = _fixture.Create<Guid>();

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UserEmailVerificationRequestedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (UserEmailVerificationRequestedCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                VerificationRequestId = Guid.Empty,
                UserId = userId,
                UserEmail = "john.doe@flowchat.local",
                ConfirmationLink = "https://localhost/confirm"
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.SourceMessageKey.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task HandleAsync_WhenUserEmailIsMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("Email is required.");

        var act = () => _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                UserEmail = " ",
                ConfirmationLink = "https://localhost/confirm"
            }.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*Email*");

        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenUserEmailLocalPartIsMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("UserName is required.");

        var act = () => _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                UserEmail = "@flowchat.local",
                ConfirmationLink = "https://localhost/confirm"
            }.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*UserName*");

        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("UserId is required.");

        var act = () => _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = Guid.Empty,
                UserEmail = "john.doe@flowchat.local",
                ConfirmationLink = "https://localhost/confirm"
            }.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*UserId*");

        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenConfirmationLinkIsMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("ConfirmationLink is required.");

        var act = () => _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                UserEmail = "john.doe@flowchat.local",
                ConfirmationLink = " "
            }.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*ConfirmationLink*");

        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UserEmailVerificationRequestedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var act = () => _subscriber.HandleAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                UserId = _fixture.Create<Guid>(),
                UserEmail = "john.doe@flowchat.local",
                ConfirmationLink = "https://localhost/confirm"
            }.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");
    }

    private void SetupCommandFailure(string errorMessage)
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UserEmailVerificationRequestedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest(errorMessage)));
    }

    private void VerifyCommandWasSent()
    {
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<UserEmailVerificationRequestedCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
