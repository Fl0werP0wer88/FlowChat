using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Application;

public sealed class PublishPresenceChangeCommandValidatorTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly PublishPresenceChangeCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsNoValidationErrors()
    {
        var command = new PublishPresenceChangeCommand(
            _fixture.Create<Guid>(),
            PresenceStatus.Active,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyUserId_ReturnsValidationError()
    {
        var command = new PublishPresenceChangeCommand(
            Guid.Empty,
            PresenceStatus.Active,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Validate_InvalidStatus_ReturnsValidationError()
    {
        var command = new PublishPresenceChangeCommand(
            _fixture.Create<Guid>(),
            (PresenceStatus)999,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Validate_RecipientUserIdsContainsOnlyEmptyGuids_ReturnsValidationError()
    {
        var command = new PublishPresenceChangeCommand(
            _fixture.Create<Guid>(),
            PresenceStatus.Active,
            DateTimeOffset.UtcNow,
            [Guid.Empty]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientUserIds");
    }

    [Fact]
    public void Validate_EmptyRecipientUserIds_ReturnsValidationError()
    {
        var command = new PublishPresenceChangeCommand(
            _fixture.Create<Guid>(),
            PresenceStatus.Active,
            DateTimeOffset.UtcNow,
            []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientUserIds");
    }
}
