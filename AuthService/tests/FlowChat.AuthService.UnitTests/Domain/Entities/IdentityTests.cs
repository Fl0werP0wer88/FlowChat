using FlowChat.AuthService.Domain.Entities.Identity;
using FlowChat.AuthService.Domain.Events;
using FluentAssertions;

namespace FlowChat.AuthService.UnitTests;

public sealed class IdentityTests
{
    [Fact]
    public void Create_WithValidEmailAndWhitespace_NormalizesValuesAndEmitsAccountRegisteredDomainEvent()
    {
        var userId = Guid.NewGuid();

        var identity = Identity.Create(userId, "  flower  ", "  flower@example.com  ");

        identity.Id.Value.Should().Be(userId);
        identity.UserName.Should().Be("flower");
        identity.Email.Should().Be("flower@example.com");
        identity.EmailConfirmed.Should().BeFalse();
        identity.AccountConfirmed.Should().BeFalse();

        identity.DomainEvents.OfType<AccountRegisteredDomainEvent>().Should().ContainSingle()
            .Which.UserId.Value.Should().Be(userId);
    }

    [Fact]
    public void Create_WithoutEmailAndPhone_ThrowsArgumentException()
    {
        var act = () => Identity.Create(Guid.NewGuid(), "flower", null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Either email or phone number is required.");
    }

    [Fact]
    public void Restore_WithConfirmedPhone_SetsAccountConfirmedWithoutDomainEvents()
    {
        var identity = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            null,
            "+48123123123",
            emailConfirmed: false,
            phoneNumberConfirmed: true);

        identity.PhoneNumberConfirmed.Should().BeTrue();
        identity.AccountConfirmed.Should().BeTrue();
        identity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmEmail_WhenEmailExists_MarksAccountConfirmedAndEmitsAccountConfirmedDomainEvent()
    {
        var identity = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            "flower@example.com",
            null,
            emailConfirmed: false,
            phoneNumberConfirmed: false);

        identity.ConfirmEmail();

        identity.EmailConfirmed.Should().BeTrue();
        identity.AccountConfirmed.Should().BeTrue();
        identity.DomainEvents.OfType<AccountConfirmedDomainEvent>().Should().ContainSingle();
        identity.DomainEvents.OfType<PhoneNumberConfirmedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public void ConfirmEmail_WhenAlreadyConfirmed_DoesNotEmitDomainEvents()
    {
        var identity = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            "flower@example.com",
            null,
            emailConfirmed: true,
            phoneNumberConfirmed: false);

        identity.ConfirmEmail();

        identity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmPhone_WhenPhoneExists_EmitsPhoneNumberConfirmedAndAccountConfirmedDomainEvents()
    {
        var identity = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            "flower@example.com",
            "+48123123123",
            emailConfirmed: false,
            phoneNumberConfirmed: false);

        identity.ConfirmPhone();

        identity.PhoneNumberConfirmed.Should().BeTrue();
        identity.AccountConfirmed.Should().BeTrue();
        identity.DomainEvents.OfType<PhoneNumberConfirmedDomainEvent>().Should().ContainSingle()
            .Which.PhoneNumber.Should().Be("+48123123123");
        identity.DomainEvents.OfType<AccountConfirmedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void ConfirmPhone_WhenPhoneNumberIsMissing_ThrowsInvalidOperationException()
    {
        var identity = Identity.Restore(
            Guid.NewGuid(),
            "flower",
            "flower@example.com",
            null,
            emailConfirmed: false,
            phoneNumberConfirmed: false);

        var act = () => identity.ConfirmPhone();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Phone confirmation requires a phone number.");
    }
}
