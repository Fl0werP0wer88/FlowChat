using AutoFixture;
using FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;
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

public sealed class AuthEmailChangedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<AuthEmailChangedSubscriber>> _loggerMock = new();
    private readonly AuthEmailChangedSubscriber _subscriber;

    public AuthEmailChangedSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ChangeAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new AuthEmailChangedSubscriber(
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenPayloadIsValid_MapsRequestToCommand()
    {
        ChangeAuthEmailCommand? capturedCommand = null;
        var userId = _fixture.Create<Guid>();

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ChangeAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (ChangeAuthEmailCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var message = new AuthEmailChangedIntegrationEvent
        {
            UserProfileId = userId,
            EmailId = _fixture.Create<Guid>(),
            EmailAddress = " john@example.com "
        };

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.EmailAddress.Should().Be("john@example.com");
    }

    [Fact]
    public async Task HandleAsync_WhenUserProfileIdIsMissing_ThrowsNonTransientException()
    {
        var message = new AuthEmailChangedIntegrationEvent
        {
            UserProfileId = Guid.Empty,
            EmailId = _fixture.Create<Guid>(),
            EmailAddress = "john@example.com"
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*UserProfileId*");
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAddressIsMissing_ThrowsNonTransientException()
    {
        var message = new AuthEmailChangedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            EmailAddress = "   "
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*EmailAddress*");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ChangeAuthEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var message = new AuthEmailChangedIntegrationEvent
        {
            UserProfileId = _fixture.Create<Guid>(),
            EmailId = _fixture.Create<Guid>(),
            EmailAddress = "john@example.com"
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");
    }
}
