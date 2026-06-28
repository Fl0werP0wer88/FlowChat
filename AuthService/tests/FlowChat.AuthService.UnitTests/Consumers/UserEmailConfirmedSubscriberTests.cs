using AutoFixture;
using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.AuthService.UnitTests;

public sealed class UserEmailConfirmedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<UserEmailConfirmedSubscriber>> _loggerMock = new();
    private readonly UserEmailConfirmedSubscriber _subscriber;

    public UserEmailConfirmedSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ConfirmAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new UserEmailConfirmedSubscriber(
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsAuth_MapsEmailToCommand()
    {
        ConfirmAuthEmailCommand? capturedCommand = null;
        var emailAddress = " john@example.com ";

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ConfirmAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (ConfirmAuthEmailCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = emailAddress,
                IsAuth = true
            }
        };

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.EmailAddress.Should().Be("john@example.com");
    }

    [Fact]
    public async Task HandleAsync_WhenEmailIsNotAuth_DoesNotSendCommand()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = false
            }
        };

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(It.IsAny<ConfirmAuthEmailCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAddressIsMissing_ThrowsNonTransientException()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "   ",
                IsAuth = true
            }
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*Email.Address*");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientExceptionAndLogsInformation()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = true
            }
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ConfirmAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");

        VerifyLog(LogLevel.Information, "Skipping UserEmailConfirmedIntegrationEvent in UserEmailConfirmedSubscriber. Reason: boom");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsTransientFailure_ThrowsTransientExceptionAndLogsWarning()
    {
        var message = new UserEmailConfirmedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            Email = new Email
            {
                Address = "john@example.com",
                IsAuth = true
            }
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ConfirmAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.UnExpected("boom", FailureKind.Transient)));

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<TransientException>()
            .WithMessage("boom");

        VerifyLog(LogLevel.Warning, "Transient failure while handling UserEmailConfirmedIntegrationEvent in UserEmailConfirmedSubscriber.");
    }

    private void VerifyLog(LogLevel expectedLogLevel, string expectedMessage)
    {
        _loggerMock.Verify(
            logger => logger.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString() == expectedMessage),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
